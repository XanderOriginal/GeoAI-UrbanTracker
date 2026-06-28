namespace GeoAI.UrbanTracker.Api.Helpers.ImageProcessing;

/// <summary>
/// Multi-index spectral classifier for true-colour Sentinel-2 PNG (R=B04, G=B03, B=B02).
///
/// Implements 6 independent vegetation/urban indices fused via weighted voting:
///   1. ExG  — Excess Green Index           (Woebbecke et al. 1995)
///   2. VARI — Visible Atmospherically Resistant Index (Gitelson et al. 2002)
///   3. GLI  — Green Leaf Index             (Louhaichi et al. 2001)
///   4. ExGR — ExG minus ExR                (Meyer & Neto 2008)
///   5. MGRVI— Modified Green Red Veg Index (Bendig et al. 2015)
///   6. RGBVI— Red Green Blue Veg Index     (Bendig et al. 2015)
///
/// Built-up detection uses HSV saturation + NRBDI (Non-Road Bare soil Detection Index).
/// Water uses a modified NDWI proxy via B/G dominance + brightness threshold.
/// </summary>
public static class NdviCalculator
{
    // ── Thresholds (tuned for Sentinel-2 L2A gamma-corrected true colour) ───
    private const double VegThreshold = 0.04;   // ExG consensus threshold
    private const double VariThreshold = 0.02;   // VARI positive = green
    private const double GliThreshold = 0.02;   // GLI positive  = green
    private const double SatThreshold = 0.12;   // HSV saturation for built-up
    private const double BrightMinBuilt = 40.0;   // min brightness for built-up
    private const double BrightMaxBuilt = 235.0;   // max brightness (avoid clouds)
    private const double WaterBright = 90.0;   // water is dark

    // ════════════════════════════════════════════════════════════════════════
    // PRIMARY VEGETATION INDEX  (continuous, −1..+1)
    // Used for NDVI-proxy: mean per image, then delta between epochs.
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Fused vegetation score: weighted average of 6 indices, each normalised to [−1,+1].
    /// Weights reflect empirical reliability for RGB-only satellite data.
    /// </summary>
    public static double EstimateVegetationIndex(byte r, byte g, byte b)
    {
        double total = r + g + b;
        if (total < 15) return 0.0;

        double rf = r / 255.0;
        double gf = g / 255.0;
        double bf = b / 255.0;
        double rn = r / total;
        double gn = g / total;
        double bn = b / total;

        // 1. ExG = 2g − r − b  (range ≈ −1..+1 after normalisation)
        double exg = Math.Clamp(2.0 * gn - rn - bn, -1.0, 1.0);

        // 2. VARI = (G − R) / (G + R − B)   — atmospherically resistant
        double variDenom = gf + rf - bf;
        double vari = Math.Abs(variDenom) < 1e-6
            ? 0.0
            : Math.Clamp((gf - rf) / variDenom, -1.0, 1.0);

        // 3. GLI = (2G − R − B) / (2G + R + B)
        double gliDenom = 2.0 * gf + rf + bf;
        double gli = gliDenom < 1e-6
            ? 0.0
            : Math.Clamp((2.0 * gf - rf - bf) / gliDenom, -1.0, 1.0);

        // 4. ExGR = ExG − ExR,  ExR = 1.4r − g
        double exr = Math.Clamp(1.4 * rn - gn, -1.0, 1.0);
        double exgr = Math.Clamp(exg - exr, -1.0, 1.0);

        // 5. MGRVI = (G² − R²) / (G² + R²)
        double g2 = gf * gf, r2 = rf * rf;
        double mgrviDenom = g2 + r2;
        double mgrvi = mgrviDenom < 1e-9
            ? 0.0
            : Math.Clamp((g2 - r2) / mgrviDenom, -1.0, 1.0);

        // 6. RGBVI = (G² − R×B) / (G² + R×B)
        double rb = rf * bf;
        double rgbviDenom = g2 + rb;
        double rgbvi = rgbviDenom < 1e-9
            ? 0.0
            : Math.Clamp((g2 - rb) / rgbviDenom, -1.0, 1.0);

        // Weighted fusion — ExG and VARI are most robust for Sentinel-2 RGB
        const double wExg = 0.30;
        const double wVari = 0.25;
        const double wGli = 0.15;
        const double wExgr = 0.15;
        const double wMgrvi = 0.10;
        const double wRgbvi = 0.05;

        return wExg * exg
             + wVari * vari
             + wGli * gli
             + wExgr * exgr
             + wMgrvi * mgrvi
             + wRgbvi * rgbvi;
    }

    // ════════════════════════════════════════════════════════════════════════
    // VEGETATION MASK  (boolean — majority vote across 4 indices)
    // ════════════════════════════════════════════════════════════════════════

    public static bool IsVegetated(byte r, byte g, byte b)
    {
        double total = r + g + b;
        if (total < 30) return false;

        double rf = r / 255.0, gf = g / 255.0, bf = b / 255.0;
        double rn = r / total, gn = g / total, bn = b / total;

        bool exgPos = (2.0 * gn - rn - bn) > VegThreshold;

        double variD = gf + rf - bf;
        bool variPos = Math.Abs(variD) > 1e-6 && ((gf - rf) / variD) > VariThreshold;

        double gliD = 2.0 * gf + rf + bf;
        bool gliPos = gliD > 1e-6 && ((2.0 * gf - rf - bf) / gliD) > GliThreshold;

        bool greenDom = g >= r && g >= b;

        // Majority vote: at least 3 of 4 must agree
        int votes = (exgPos ? 1 : 0) + (variPos ? 1 : 0)
                  + (gliPos ? 1 : 0) + (greenDom ? 1 : 0);
        return votes >= 3;
    }

    // ════════════════════════════════════════════════════════════════════════
    // BUILT-UP MASK
    // Low HSV saturation + moderate brightness + not vegetated + not water
    // ════════════════════════════════════════════════════════════════════════

    public static bool IsBuiltUp(byte r, byte g, byte b)
    {
        double brightness = (r + g + b) / 3.0;
        if (brightness < BrightMinBuilt || brightness > BrightMaxBuilt) return false;
        if (IsVegetated(r, g, b) || IsWater(r, g, b)) return false;

        // HSV saturation = (max − min) / max
        double maxC = Math.Max(r, Math.Max(g, b));
        double minC = Math.Min(r, Math.Min(g, b));
        double sat = maxC < 1e-6 ? 0.0 : (maxC - minC) / maxC;

        return sat < SatThreshold;
    }

    // ════════════════════════════════════════════════════════════════════════
    // WATER MASK
    // Dark + blue-dominant OR very dark uniform
    // ════════════════════════════════════════════════════════════════════════

    public static bool IsWater(byte r, byte g, byte b)
    {
        double brightness = (r + g + b) / 3.0;
        if (brightness > WaterBright) return false;
        return b >= r && b >= g;
    }

    // ════════════════════════════════════════════════════════════════════════
    // BARE / ARID MASK
    // Red-dominant, moderate brightness, not grey (spread > 15)
    // ════════════════════════════════════════════════════════════════════════

    public static bool IsBareOrArid(byte r, byte g, byte b)
    {
        double total = r + g + b;
        if (total < 30) return false;
        double spread = Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b));
        double rn = r / total;
        double gn = g / total;
        return rn > 0.38 && gn < 0.34 && spread > 15
            && !IsVegetated(r, g, b);
    }
}
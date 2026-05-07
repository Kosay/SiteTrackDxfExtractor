using System;
using System.Collections.Generic;

namespace DxfCoordinateExtractor;

/// <summary>
/// Top-level container for data extracted from AutoCAD
/// </summary>
public class AutoCADExportData
{
    public int SchemaVersion { get; set; } = 1;
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = "AutoCAD";
    public string CoordinateSystemHint { get; set; } = "UTM";

    public List<PointData> Points { get; set; } = new();
    public List<LineData> Lines { get; set; } = new();
    public List<CurveData> Curves { get; set; } = new();
    public List<TextData> Texts { get; set; } = new();
}

/// <summary>
/// Point data (manhole, junction, intersection, etc.)
/// </summary>
public class PointData
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public double E { get; set; }
    public double N { get; set; }
    public string Layer { get; set; } = "";
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Line data (pipe, road, etc.)
/// Uses explicit coordinates instead of point references
/// </summary>
public class LineData
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";

    // Explicit coordinates (not relying on point references)
    public double StartE { get; set; }
    public double StartN { get; set; }
    public double EndE { get; set; }
    public double EndN { get; set; }

    public double Length { get; set; }
    public string Layer { get; set; } = "";
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Calculate length from coordinates if not provided
    /// </summary>
    public void CalculateLength()
    {
        var dx = EndE - StartE;
        var dy = EndN - StartN;
        Length = Math.Sqrt(dx * dx + dy * dy);
    }
}

/// <summary>
/// Curve data (arc, circle, etc.)
/// </summary>
public class CurveData
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string EntityType { get; set; } = "ARC"; // "ARC" or "CIRCLE"

    public double CenterE { get; set; }
    public double CenterN { get; set; }
    public double Radius { get; set; }
    public double StartAngle { get; set; }
    public double EndAngle { get; set; }

    public string Layer { get; set; } = "";
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Text data (labels, annotations, etc.)
/// </summary>
public class TextData
{
    public string Id { get; set; } = "";
    public string Content { get; set; } = "";
    public double E { get; set; }
    public double N { get; set; }
    public string Layer { get; set; } = "";
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Summary statistics for extracted data
/// </summary>
public class ExtractionSummary
{
    public int PointCount { get; set; }
    public int LineCount { get; set; }
    public int CurveCount { get; set; }
    public int TextCount { get; set; }
    public int TotalCount => PointCount + LineCount + CurveCount + TextCount;
}

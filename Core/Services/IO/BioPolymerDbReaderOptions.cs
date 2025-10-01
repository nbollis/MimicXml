using UsefulProteomicsDatabases;

namespace Core.Services.IO;

public class BioPolymerDbReaderOptions
{
    public string DecoyIdentifier { get; set; } = "DECOY"; // Default for many tools, can be overridden
    public string EntrapmentIdentifier { get; set; } = "NTRAP"; 
    public bool GenerateDecoys => DecoyType == DecoyType.None;
    public DecoyType DecoyType { get; set; } = DecoyType.None;
    public bool AddTruncations { get; set; } = false; // Likely will never use
   
    // Sequence Variant Information
    public int MaxHeterozygousVariants { get; set; } = 4;
    public int MinAlleleDepth { get; set; } = 1;

    // Maybe RNA-specific transforms:
    public bool ConvertTtoU { get; set; } = false;
}
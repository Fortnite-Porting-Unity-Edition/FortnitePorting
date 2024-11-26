using System.ComponentModel;

public enum EFileCompressionFormat
{
    [Description("Uncompressed")]
    None,
    
    [Description("Gzip Compression")]
    GZIP,
    
    [Description("ZStandard Compression")]
    ZSTD
}
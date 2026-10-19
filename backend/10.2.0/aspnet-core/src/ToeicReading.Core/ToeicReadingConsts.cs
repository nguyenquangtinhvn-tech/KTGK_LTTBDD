using ToeicReading.Debugging;

namespace ToeicReading;

public class ToeicReadingConsts
{
    public const string LocalizationSourceName = "ToeicReading";

    public const string ConnectionStringName = "Default";

    public const bool MultiTenancyEnabled = true;


    /// <summary>
    /// Default pass phrase for SimpleStringCipher decrypt/encrypt operations
    /// </summary>
    public static readonly string DefaultPassPhrase =
        DebugHelper.IsDebug ? "gsKxGZ012HLL3MI5" : "58086c96e27247b19f7f5b4b8023591c";
}

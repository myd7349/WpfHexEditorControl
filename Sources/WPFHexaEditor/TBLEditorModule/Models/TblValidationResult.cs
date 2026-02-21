//////////////////////////////////////////////
// Apache 2.0  - 2003-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

namespace WpfHexaEditor.TBLEditorModule.Models
{
    /// <summary>
    /// Result of TBL entry validation
    /// </summary>
    public class TblValidationResult
    {
        /// <summary>
        /// Whether the entry is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Error message if invalid (null if valid)
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Warning message (non-critical issues)
        /// </summary>
        public string WarningMessage { get; set; }

        /// <summary>
        /// Create successful validation result
        /// </summary>
        public static TblValidationResult Success() => new() { IsValid = true };

        /// <summary>
        /// Create failed validation result with error
        /// </summary>
        public static TblValidationResult Error(string message) =>
            new() { IsValid = false, ErrorMessage = message };

        /// <summary>
        /// Create validation result with warning
        /// </summary>
        public static TblValidationResult Warning(string message) =>
            new() { IsValid = true, WarningMessage = message };
    }
}

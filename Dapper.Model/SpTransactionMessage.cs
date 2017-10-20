using System.Runtime.InteropServices;

namespace Dapper.Model
{
    public class SpTransactionMessage
    {
        
        public int Number { get; set; }
        public int Severity  { get; set; }
       // public string State  { get; set; }
        public string StoredProcedureName  { get; set; }
        public int LineNumber { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
    }
}
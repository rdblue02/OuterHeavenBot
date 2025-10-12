using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenLight.Core
{
    public struct CommandResult
    {
        public bool Success { get; } 
        public string? UserMessage { get; }  
        public Exception? Exception { get;  }  
        
        public CommandResult(bool success, string? userMessage, Exception? exception)
        {
            Success = success;
            UserMessage = userMessage;
            Exception = exception;
        }

        public static CommandResult SuccessResult(string? userMessage = "") => new CommandResult(true, userMessage, null);
        public static CommandResult FailureResult(string? userMessage,Exception? exception = null) => new CommandResult(false, userMessage, exception);
    }
}

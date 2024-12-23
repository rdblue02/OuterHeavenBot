using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterHeavenBot.Core.Models
{
    public class CommandResult<T> : CommandResult
    {
        public T? ResultData { get; set; } = default;

    }

    public class CommandResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}

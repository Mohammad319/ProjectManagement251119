using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Wrappers
{
    public class Response<T>
    {
        public Response()
        {
            Message = string.Empty;
            Errors = [];
            Data = default!;
        }

        public Response(T data, string? message = null)
        {
            Succeeded = true;
            Message = message ?? string.Empty;
            Errors = [];
            Data = data;
        }

        public Response(string message)
        {
            Succeeded = false;
            Message = message;
            Errors = [];
            Data = default!;
        }

        public bool Succeeded { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; }
        public T Data { get; set; }
    }
}

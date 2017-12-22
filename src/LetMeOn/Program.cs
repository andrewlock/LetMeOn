using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace AspNetCoreInDocker.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = WebHost.Start(ctx =>
            {
                ctx.Response.ContentType = "text/html";
                return ctx.Response.WriteAsync(@"<!DOCTYPE html><html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml""><head> <meta charset=""utf-8""/> <title>LetMeOn.Ste</title> <style>html, body{width: 100%; height: 100%;}html{display: table;}body{display: table-cell; vertical-align: middle; text-align: center; background: linear-gradient(135deg, rgba(30,87,153,1) 0%,rgba(41,137,216,1) 50%,rgba(125,185,232,1) 100%);}h1{font-family: 'Gill Sans', 'Gill Sans MT', Calibri, 'Trebuchet MS', sans-serif; color: #fff;}</style></head><body> <h1>Hooray, you're online!</h1></body></html>");
            });
            host.WaitForShutdown();
        }
    }
}

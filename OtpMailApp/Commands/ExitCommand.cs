using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtpMailApp.Commands;

public class ExitCommand : ICommand
{
    public void Execute()
    {
        Console.WriteLine("Exiting...");
        Environment.Exit(0);
    }
}

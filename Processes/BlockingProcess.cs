
using System;
using System.Diagnostics;
using System.Text;

class BlockingProcess {

    public ProcessStartInfo StartInfo { get; set; }

    public Tuple<int, string> Run() {

        var process = new Process() {
            EnableRaisingEvents = true,
        };
        process.StartInfo = StartInfo;
        StringBuilder output = new StringBuilder();
        process.ErrorDataReceived += (sender, message) => {
            output.AppendLine(message.Data);
        };

        process.OutputDataReceived += (sender, message) => {
            output.AppendLine(message.Data);
        };
        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();
        process.WaitForExit();
        return new Tuple<int, string>(process.ExitCode, output.ToString());

    }

}

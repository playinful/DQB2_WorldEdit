using Godot;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

public partial class ProcessTest_Label : Label
{
	const int PROCESS_WM_READ = 0x0010;

	[DllImport("kernel32.dll")]
	public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
	[DllImport("kernel32.dll")]
    public static extern bool ReadProcessMemory(int hProcess, long lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

	private Process DragonQuestBuilders2;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GetProcess();
	}
	private void GetProcess()
	{
		Process[] processes = Process.GetProcessesByName("DQB2_EU");
		if (processes.Length > 0)
		{
			DragonQuestBuilders2 = processes[0];
		}
	}
	private void ReadNotepadData()
	{
		Process process = Process.GetProcessesByName("notepad")[0]; 
        IntPtr processHandle = OpenProcess(PROCESS_WM_READ, false, process.Id); 

        int bytesRead = 0;
        byte[] buffer = new byte[24]; //'Hello World!' takes 12*2 bytes because of Unicode 

        // 0x0046A3B8 is the address where I found the string, replace it with what you found
        ReadProcessMemory((int)processHandle, 0x29F599A2DA0, buffer, buffer.Length, ref bytesRead);

        GD.Print(Encoding.Unicode.GetString(buffer) + " (" + bytesRead.ToString() + "bytes)");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (DragonQuestBuilders2 is null)
			return;

        IntPtr processHandle = OpenProcess(PROCESS_WM_READ, false, DragonQuestBuilders2.Id); 

        int bytesRead = 0;
        byte[] buffer = new byte[4];

        ReadProcessMemory((int)processHandle, 0x2726D080EA4, buffer, buffer.Length, ref bytesRead);

        Text = $"Gratitude: {BitConverter.ToInt32(buffer, 0)}";
	}
}

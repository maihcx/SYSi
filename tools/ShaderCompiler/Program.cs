using System.Runtime.InteropServices;
using System.Text;

internal static class Program
{
    private const uint D3DCOMPILE_DEBUG = 1u << 0;
    private const uint D3DCOMPILE_SKIP_OPTIMIZATION = 1u << 2;
    private const uint D3DCOMPILE_ENABLE_STRICTNESS = 1u << 11;
    private const uint D3DCOMPILE_OPTIMIZATION_LEVEL3 = 1u << 15;

    // Giá trị đặc biệt được Direct3D định nghĩa để bật trình xử lý #include chuẩn.
    private static readonly IntPtr D3D_COMPILE_STANDARD_FILE_INCLUDE = new(1);

    private static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            PrintUsage();
            return 1;
        }

        string inputPath = Path.GetFullPath(args[0]);
        string outputPath = Path.GetFullPath(args[1]);
        string entryPoint = args.Length > 2 ? args[2] : "main";
        string target = args.Length > 3 ? args[3] : "ps_3_0";
        bool debug = args.Any(
            argument => string.Equals(
                argument,
                "--debug",
                StringComparison.OrdinalIgnoreCase));

        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Không tìm thấy shader HLSL: {inputPath}");
            return 1;
        }

        if (!IsSupportedTarget(target))
        {
            Console.Error.WriteLine(
                $"Profile '{target}' không hợp lệ cho WPF ShaderEffect. " +
                "Hãy dùng ps_2_0 hoặc ps_3_0.");
            return 1;
        }

        try
        {
            byte[] source = ReadShaderSource(inputPath);

            uint flags = 0;

            if (debug)
            {
                flags |= D3DCOMPILE_DEBUG;
                flags |= D3DCOMPILE_SKIP_OPTIMIZATION;
            }
            else
            {
                flags |= D3DCOMPILE_OPTIMIZATION_LEVEL3;
            }

            int result = Compile(
                source,
                inputPath,
                entryPoint,
                target,
                flags,
                out byte[]? bytecode,
                out string? errorMessage);

            if (result != 0 || bytecode is null)
            {
                Console.Error.WriteLine("Lỗi biên dịch HLSL:");
                Console.Error.WriteLine(
                    string.IsNullOrWhiteSpace(errorMessage)
                        ? $"D3DCompile thất bại, HRESULT 0x{result:X8}"
                        : errorMessage);

                return 1;
            }

            string? outputDirectory = Path.GetDirectoryName(outputPath);

            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            File.WriteAllBytes(outputPath, bytecode);

            Console.WriteLine(
                $"OK: {Path.GetFileName(inputPath)} -> {outputPath}");
            Console.WriteLine(
                $"Entry point: {entryPoint}, target: {target}, " +
                $"size: {bytecode.Length:N0} bytes");

            return 0;
        }
        catch (DllNotFoundException)
        {
            Console.Error.WriteLine(
                "Không tìm thấy d3dcompiler_47.dll. " +
                "Ứng dụng cần chạy trên Windows 8.1/10/11.");
            return 1;
        }
        catch (BadImageFormatException)
        {
            Console.Error.WriteLine(
                "Không thể nạp d3dcompiler_47.dll. " +
                "Hãy kiểm tra kiến trúc tiến trình và hệ điều hành.");
            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Lỗi: {exception.Message}");
            return 1;
        }
    }

    private static int Compile(
        byte[] source,
        string sourcePath,
        string entryPoint,
        string target,
        uint flags,
        out byte[]? bytecode,
        out string? errorMessage)
    {
        IntPtr codeBlob = IntPtr.Zero;
        IntPtr errorBlob = IntPtr.Zero;

        try
        {
            int hr = D3DCompile(
                source,
                (nuint)source.Length,
                sourcePath,
                IntPtr.Zero,
                D3D_COMPILE_STANDARD_FILE_INCLUDE,
                entryPoint,
                target,
                flags,
                0,
                out codeBlob,
                out errorBlob);

            errorMessage = ReadBlobAsAnsiString(errorBlob);

            if (hr < 0 || codeBlob == IntPtr.Zero)
            {
                bytecode = null;
                return hr;
            }

            nuint nativeSize = GetBufferSize(codeBlob);

            if (nativeSize > int.MaxValue)
            {
                bytecode = null;
                errorMessage =
                    $"Bytecode shader quá lớn: {nativeSize:N0} bytes.";
                return unchecked((int)0x8007000E);
            }

            int size = checked((int)nativeSize);
            bytecode = new byte[size];

            Marshal.Copy(
                GetBufferPointer(codeBlob),
                bytecode,
                0,
                size);

            return hr;
        }
        finally
        {
            ReleaseBlob(codeBlob);
            ReleaseBlob(errorBlob);
        }
    }

    private static byte[] ReadShaderSource(string inputPath)
    {
        byte[] source = File.ReadAllBytes(inputPath);

        // D3DCompile xử lý source dạng byte, nhưng loại bỏ UTF-8 BOM giúp
        // tránh lỗi ở một số shader/compiler cũ.
        if (source.Length >= 3 &&
            source[0] == 0xEF &&
            source[1] == 0xBB &&
            source[2] == 0xBF)
        {
            return source[3..];
        }

        return source;
    }

    private static bool IsSupportedTarget(string target)
    {
        return string.Equals(target, "ps_2_0", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(target, "ps_3_0", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ReadBlobAsAnsiString(IntPtr blob)
    {
        if (blob == IntPtr.Zero)
        {
            return null;
        }

        IntPtr pointer = GetBufferPointer(blob);
        nuint nativeSize = GetBufferSize(blob);

        if (pointer == IntPtr.Zero || nativeSize == 0)
        {
            return null;
        }

        int size = checked((int)Math.Min(nativeSize, (nuint)int.MaxValue));
        byte[] bytes = new byte[size];

        Marshal.Copy(pointer, bytes, 0, size);

        int terminatorIndex = Array.IndexOf(bytes, (byte)0);

        if (terminatorIndex >= 0)
        {
            size = terminatorIndex;
        }

        return Encoding.Default.GetString(bytes, 0, size).Trim();
    }

    private static void ReleaseBlob(IntPtr blob)
    {
        if (blob != IntPtr.Zero)
        {
            Marshal.Release(blob);
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            """
            ShaderCompiler - biên dịch HLSL bằng d3dcompiler_47.dll

            Cách dùng:
              ShaderCompiler <input.hlsl> <output.ps> [entryPoint] [target] [--debug]

            Ví dụ:
              ShaderCompiler LiquidGlass.hlsl LiquidGlass.ps
              ShaderCompiler LiquidGlass.hlsl LiquidGlass.ps main ps_3_0
              ShaderCompiler ChromaticAberration.fx ChromaticAberration.ps main ps_2_0
              ShaderCompiler LiquidGlass.hlsl LiquidGlass.ps main ps_3_0 --debug

            Profile hỗ trợ cho WPF ShaderEffect:
              ps_2_0
              ps_3_0
            """);
    }

    [DllImport(
        "d3dcompiler_47.dll",
        EntryPoint = "D3DCompile",
        CallingConvention = CallingConvention.StdCall,
        CharSet = CharSet.Ansi)]
    private static extern int D3DCompile(
        byte[] pSrcData,
        nuint srcDataSize,
        [MarshalAs(UnmanagedType.LPStr)] string pSourceName,
        IntPtr pDefines,
        IntPtr pInclude,
        [MarshalAs(UnmanagedType.LPStr)] string pEntryPoint,
        [MarshalAs(UnmanagedType.LPStr)] string pTarget,
        uint flags1,
        uint flags2,
        out IntPtr ppCode,
        out IntPtr ppErrorMsgs);

    private static IntPtr GetBufferPointer(IntPtr blob)
    {
        IntPtr vtable = Marshal.ReadIntPtr(blob);
        IntPtr functionPointer =
            Marshal.ReadIntPtr(vtable, IntPtr.Size * 3);

        GetBufferPointerDelegate function =
            Marshal.GetDelegateForFunctionPointer<GetBufferPointerDelegate>(
                functionPointer);

        return function(blob);
    }

    private static nuint GetBufferSize(IntPtr blob)
    {
        IntPtr vtable = Marshal.ReadIntPtr(blob);
        IntPtr functionPointer =
            Marshal.ReadIntPtr(vtable, IntPtr.Size * 4);

        GetBufferSizeDelegate function =
            Marshal.GetDelegateForFunctionPointer<GetBufferSizeDelegate>(
                functionPointer);

        return function(blob);
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr GetBufferPointerDelegate(IntPtr self);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate nuint GetBufferSizeDelegate(IntPtr self);
}

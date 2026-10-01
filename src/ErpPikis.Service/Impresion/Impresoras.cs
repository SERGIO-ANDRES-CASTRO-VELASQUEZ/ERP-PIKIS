using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ErpPikis.Service.Exceptions;

namespace ErpPikis.Service.Impresion;

public interface IImpresoraTermica
{
    Task EnviarAsync(byte[] datos, CancellationToken ct = default);
}

public class ImpresoraNoConfigurada : IImpresoraTermica
{
    public Task EnviarAsync(byte[] datos, CancellationToken ct = default) =>
        throw new ImpresoraException("No hay impresora térmica configurada (ImpresoraTermica:Modo = Ninguna).");
}

public class ImpresoraRed : IImpresoraTermica
{
    private readonly ImpresoraOptions _opciones;

    public ImpresoraRed(ImpresoraOptions opciones)
    {
        if (string.IsNullOrWhiteSpace(opciones.Host))
            throw new InvalidOperationException("ImpresoraTermica:Host es obligatorio cuando Modo = Red.");
        _opciones = opciones;
    }

    public async Task EnviarAsync(byte[] datos, CancellationToken ct = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(_opciones.TimeoutSegundos));

        try
        {
            using var cliente = new TcpClient();
            await cliente.ConnectAsync(_opciones.Host!, _opciones.Puerto, timeout.Token);
            await using var stream = cliente.GetStream();
            await stream.WriteAsync(datos, timeout.Token);
            await stream.FlushAsync(timeout.Token);
        }
        catch (Exception e) when (e is SocketException or IOException or OperationCanceledException && !ct.IsCancellationRequested)
        {
            throw new ImpresoraException(
                $"No se pudo conectar con la impresora {_opciones.Host}:{_opciones.Puerto}. Verifique que esté encendida y en red.", e);
        }
    }
}

public class ImpresoraWindows : IImpresoraTermica
{
    private readonly ImpresoraOptions _opciones;

    public ImpresoraWindows(ImpresoraOptions opciones)
    {
        if (string.IsNullOrWhiteSpace(opciones.NombreImpresora))
            throw new InvalidOperationException("ImpresoraTermica:NombreImpresora es obligatorio cuando Modo = Windows.");
        _opciones = opciones;
    }

    public Task EnviarAsync(byte[] datos, CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows())
            throw new ImpresoraException("El modo de impresión 'Windows' solo funciona cuando el backend corre en Windows.");

        ct.ThrowIfCancellationRequested();
        SpoolerRaw.Enviar(_opciones.NombreImpresora!, datos);
        return Task.CompletedTask;
    }
}

[SupportedOSPlatform("windows")]
internal static class SpoolerRaw
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private class DocInfo1
    {
        public string DocName = "ERP-PIKIS Tiquete";
        public string? OutputFile;
        public string DataType = "RAW";
    }

    [DllImport("winspool.drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool OpenPrinter(string printerName, out IntPtr handle, IntPtr defaults);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr handle);

    [DllImport("winspool.drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool StartDocPrinter(IntPtr handle, int level, [In] DocInfo1 docInfo);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr handle);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr handle);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr handle);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr handle, byte[] bytes, int count, out int written);

    public static void Enviar(string nombreImpresora, byte[] datos)
    {
        if (!OpenPrinter(nombreImpresora, out var handle, IntPtr.Zero))
            throw Error($"No se encontró la impresora '{nombreImpresora}' en Windows");

        try
        {
            if (!StartDocPrinter(handle, 1, new DocInfo1()))
                throw Error("No se pudo iniciar el documento");
            try
            {
                if (!StartPagePrinter(handle))
                    throw Error("No se pudo iniciar la página");
                if (!WritePrinter(handle, datos, datos.Length, out var escritos) || escritos != datos.Length)
                    throw Error("No se pudieron enviar los datos");
                EndPagePrinter(handle);
            }
            finally
            {
                EndDocPrinter(handle);
            }
        }
        finally
        {
            ClosePrinter(handle);
        }
    }

    private static ImpresoraException Error(string mensaje) =>
        new($"{mensaje}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");
}

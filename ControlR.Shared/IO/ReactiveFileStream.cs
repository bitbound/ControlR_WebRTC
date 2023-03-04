using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Shared.IO;
public class ReactiveFileStream : FileStream
{
    private int _totalRead = 0;

    private int _totalWritten = 0;

    public ReactiveFileStream(string path, FileMode mode) :
        base(path, mode)
    {
    }

    public event EventHandler<int>? TotalBytesReadChanged;

    public event EventHandler<int>? TotalBytesWrittenChanged;

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = base.Read(buffer, offset, count);
        _totalRead += read;
        TotalBytesReadChanged?.Invoke(this, _totalRead);
        return read;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        base.Write(buffer, offset, count);
        _totalWritten += count;
        TotalBytesWrittenChanged?.Invoke(this, _totalWritten);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            TotalBytesReadChanged = null;
            TotalBytesWrittenChanged = null;
        }

        base.Dispose(disposing);
    }
}
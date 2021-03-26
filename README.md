# NC-DokanFsBase
This library will help you create new Dokan User Mode File System quicker and easier.

## Why you need this library?

When implementing new File System for Dokan, you would be implementing `IDokanOperations` interface and writing roughly 24 functions.

Although the functions in `IDokanOperations` seems trivial, you would face a lot of trouble creating a working file system because you do not know how to correctly respond to each calls made to your `IDokanOperations`.

Fortunately, Dokan-net has provided [Mirror.cs](https://github.com/dokan-dev/dokan-dotnet/blob/master/sample/DokanNetMirror/Mirror.cs) which seems to have encapsulated the knowledge of creating a working file system in Dokan. The Mirror sample
use File/Directory/Path classes in `System.IO` namespace to handle the requests.

I have extracted those calls into an interface called `IDokanDisk` which is available to you in this Library. So you can implement a working File system without having an in-depth understanding of
why **`CreateFile`** function gets called when Windows Explorer trying to **list** files in a folder!

## Implementation Steps

1. Create a new class which implements `IDokanDisk`. There are 22 functions to implement, just be patient and follow the guide in XML Comments.
Trust me, it's a lot more trivial than having to implement `CreateFile` function in `IDokanOperations` which does almost everything.

```cs
using NC.DokanFS;

namespace NC.CrossDrive.App
{
    public class CrossDriveDokanDisk : IDokanDisk
    {
        // implement the operations here
    }
}

```

2. Create a new class which implments `IDokanFileContext`. Your `IDokanDisk`'s **`CreateFileContext`** function should return a new instance of this class.

This class is where you will perform read/write operations to your backend storage. If you do not want to support an operation, just throw `IOException`

`Flush` method will be called when the application request the write buffer to be flushed - so do that!

The `Dispose` method of this class will gets called when an application using the file close the file handle. So in `Dispose`, make sure to clean up everything related to the file.

```cs
using NC.DokanFS;
namespace NC.CrossDrive.App
{
    public class CrossDriveFileContext : IDokanFileContext
    {
        // implement Read, Close, Flush Dispose
    }
}
```

3. Create new instance of `DokanFrontend` and pass an instance of your `IDokanDisk` to it. `DokanFrontend` class is the modified Mirror.cs sample which calls to `IDokanDisk` instead of vairous `System.IO.*` classes.

```cs
using NC.DokanFS;
namespace NC.CrossDrive.App
{
    class Program
    {
        static void Main(string[] args)
        {
            var crossdrive = new CrossDriveDokanDisk();
            var dokan = new DokanFrontend(crossdrive, "CrossDrive");
            dokan.Mount(@"H:\");

            Console.WriteLine("Press ENTER to Exit.");
            Console.Read();

            dokan.Unmount();
        }
    }
}
```

Run and enjoy your new file system!

## Base Classes in NC.DokanFS.Specialized

There are few helpful classes in this namespace to help you get started even faster. 

- **`ReadOnlyDiskBase`** : inherit this class to create a read-only file system
- **`ReadOnlyFileContextBase`** : inherit this class for read-only file context
- **`BlockBasedReadOnlyFileContextBase`** : inherit this class for read-only file context which fetch data from backend storage in blocks
- **`BlockBasedFileContextBase`** : inherit this class for file context which fetch/write data from backend storage in blocks

## License

MIT License.





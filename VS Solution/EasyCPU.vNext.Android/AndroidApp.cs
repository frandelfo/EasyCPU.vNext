using System;
using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using EasyCPU.vNext;
using ReactiveUI.Avalonia;

namespace EasyCPU.vNext.Android;

[Application]
public class AndroidApp : AvaloniaAndroidApplication<App>
{
    protected AndroidApp(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer) { }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .UseReactiveUI(_ => { });
    }
}

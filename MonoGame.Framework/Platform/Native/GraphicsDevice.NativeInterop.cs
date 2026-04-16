// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MonoGame.Framework.Utilities;
using MonoGame.Interop;

namespace Microsoft.Xna.Framework.Graphics;

public partial class GraphicsDevice
{
    /// <summary>
    /// Sets required Vulkan extensions before the graphics device is created.
    /// Must be called before <see cref="Game.Run"/>.
    /// No-op on non-Vulkan backends.
    /// </summary>
    /// <param name="instanceExtensions">Space-separated Vulkan instance extension names, or null.</param>
    /// <param name="deviceExtensions">Space-separated Vulkan device extension names, or null.</param>
    public static void SetRequiredExtensions(string? instanceExtensions, string? deviceExtensions)
    {
        MGG.GraphicsDevice_SetRequiredExtensions(instanceExtensions, deviceExtensions);
    }

    /// <summary>
    /// Retrieves the native graphics API handles for XR or external interop.
    /// <see cref="NativeGraphicsHandles.Device"/> and <see cref="NativeGraphicsHandles.Queue"/>
    /// are always populated. Check <see cref="NativeGraphicsHandles.Backend"/>
    /// for backend-specific field availability.
    /// </summary>
    public unsafe NativeGraphicsHandles GetNativeHandles()
    {
        MGG.GraphicsDevice_GetNativeHandles(Handle, out var native);

        return new NativeGraphicsHandles(
            (GraphicsBackend)native.Backend,
            native.Instance,
            native.PhysicalDevice,
            native.Device,
            native.Queue,
            native.QueueFamilyIndex,
            native.QueueIndex);
    }
}

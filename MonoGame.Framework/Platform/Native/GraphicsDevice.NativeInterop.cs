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
        return new NativeGraphicsHandles
        {
            Backend          = (GraphicsBackend)native.Backend,
            Instance         = native.Instance,
            PhysicalDevice   = native.PhysicalDevice,
            Device           = native.Device,
            Queue            = native.Queue,
            QueueFamilyIndex = native.QueueFamilyIndex,
            QueueIndex       = native.QueueIndex,
        };
    }

    /// <summary>
    /// Copies image data between two native images with appropriate layout/state transitions.
    /// Used for XR swapchain image submission. Flushes active GPU commands before copying.
    /// </summary>
    /// <param name="source">Source native image handle (VkImage or ID3D12Resource*).</param>
    /// <param name="destination">Destination native image handle.</param>
    /// <param name="sourceLayout">Current layout/state of the source image.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    public unsafe void CopyNativeImage(
        nint source,
        nint destination,
        NativeImageLayout sourceLayout,
        int width,
        int height)
    {
        MGG.GraphicsDevice_CopyImage(Handle,
            source,
            destination,
            (int)sourceLayout,
            width,
            height);
    }
}

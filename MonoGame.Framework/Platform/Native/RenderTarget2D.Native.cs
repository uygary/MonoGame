// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MonoGame.Interop;
namespace Microsoft.Xna.Framework.Graphics;

public partial class RenderTarget2D
{
    private unsafe void PlatformConstruct(GraphicsDevice graphicsDevice, int width, int height, bool mipMap, DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage, bool shared)
    {
        Handle = MGG.RenderTarget_Create(
            GraphicsDevice.Handle,
            TextureType._2D,
            _format,
            width,
            height,
            1,
            _levelCount,
            ArraySize,
            preferredDepthFormat,
            preferredMultiSampleCount,
            usage);
    }

    private unsafe void PlatformGraphicsDeviceResetting()
    {
        if (Handle != null && Owned)
        {
            MGG.Texture_Destroy(GraphicsDevice.Handle, Handle);
            Handle = null;
        }
    }

    /// <summary>
    /// Creates a <see cref="RenderTarget2D"/> that wraps an externally-owned native image
    /// (e.g. an OpenXR swapchain <c>VkImage</c> or <c>ID3D12Resource*</c>).
    /// The returned render target does NOT own the native image memory —
    /// the caller is responsible for its lifetime.
    /// </summary>
    /// <param name="graphicsDevice">The MonoGame graphics device.</param>
    /// <param name="nativeImageHandle">The native image handle (<c>VkImage</c> cast to <see cref="nint"/>, or <c>ID3D12Resource*</c>).</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="format">The surface format of the native image.</param>
    /// <param name="depthFormat">Depth format for an internally-created depth buffer, or <see cref="DepthFormat.None"/>.</param>
    /// <param name="multiSampleCount">MSAA sample count (0 for none).</param>
    /// <returns>A non-owning <see cref="RenderTarget2D"/> backed by the native image.</returns>
    public static unsafe RenderTarget2D FromNativeImage(
        GraphicsDevice graphicsDevice,
        nint nativeImageHandle,
        int width,
        int height,
        SurfaceFormat format = SurfaceFormat.Color,
        DepthFormat depthFormat = DepthFormat.Depth24Stencil8,
        int multiSampleCount = 0)
    {
        // Call native layer to create an MGG_Texture that wraps the external image.
        // This creates VkImageViews (and optionally a depth buffer) without allocating the color image.
        var handle = MGG.RenderTarget_WrapNativeImage(
            graphicsDevice.Handle,
            nativeImageHandle,
            format,
            width,
            height,
            depthFormat,
            multiSampleCount);

        // Use the protected constructor that takes SurfaceType.SwapChainRenderTarget,
        // which skips PlatformConstruct (no native RT creation).
        var rt = new RenderTarget2D(
            graphicsDevice,
            width,
            height,
            false,
            format,
            depthFormat,
            multiSampleCount,
            RenderTargetUsage.DiscardContents,
            SurfaceType.SwapChainRenderTarget);

        // Assign the natively-wrapped handle. Owned = false prevents Dispose from destroying
        // the OpenXR-owned VkImage. The MGG_Texture struct (views + depth) WILL be cleaned up
        // since MGG_Texture_Destroy now only skips vmaDestroyImage for null allocations.
        rt.Handle = handle;
        rt.Owned = true; // We DO own the MGG_Texture* wrapper (views, depth buffer) — just not the VkImage

        return rt;
    }

    /// <summary>
    /// Updates the native image pointer on a render target previously created with
    /// <see cref="FromNativeImage"/>. This is used for swapchain image rotation
    /// (e.g. after <c>xrAcquireSwapchainImage</c> returns a new image index).
    /// Destroys and recreates the image views. Does not touch the depth buffer.
    /// </summary>
    /// <param name="newNativeImageHandle">The new native image handle.</param>
    public unsafe void UpdateNativeImage(nint newNativeImageHandle)
    {
        if (Handle == null)
        {
            return;
        }

        MGG.RenderTarget_UpdateNativeImage(Handle, newNativeImageHandle, GraphicsDevice.Handle);
    }
}

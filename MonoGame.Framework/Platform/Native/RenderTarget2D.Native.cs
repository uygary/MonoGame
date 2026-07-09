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
    /// Creates a <see cref="RenderTarget2D"/> that wraps an externally-owned native image.
    /// Like an OpenXR swap-chain <c>VkImage</c> or <c>ID3D12Resource*</c>.
    /// </summary>
    /// <param name="graphicsDevice">The MonoGame graphics device.</param>
    /// <param name="nativeImageHandle">The native image handle. <para>This should be <c>VkImage</c> cast to <see cref="nint"/> for Vulkan, or <c>ID3D12Resource*</c> for DX12.</para></param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="format">The surface format of the native image.</param>
    /// <param name="preferredDepthFormat">The preferred depth format of the render target.<para><see cref="DepthFormat.Depth24Stencil8"/> by default.</para></param>
    /// <param name="preferredMultiSampleCount">The preferred number of samples per pixel when multisampling.<para><c>0</c> by default.</para></param>
    /// <remarks>
    /// WARNING: The returned render target doesn't own the native image.
    /// The caller is responsible for its lifetime.
    /// </remarks>
    /// <returns>A non-owning <see cref="RenderTarget2D"/> backed by the native image.</returns>
    public static unsafe RenderTarget2D FromNativeImage(
        GraphicsDevice graphicsDevice,
        nint nativeImageHandle,
        int width,
        int height,
        SurfaceFormat format = SurfaceFormat.Color,
        DepthFormat preferredDepthFormat = DepthFormat.Depth24Stencil8,
        int preferredMultiSampleCount = 0)
    {
        // Call native layer to create an MGG_Texture that wraps the external image.
        var handle = MGG.RenderTarget_WrapNativeImage(
            graphicsDevice.Handle,
            nativeImageHandle,
            format,
            width,
            height,
            preferredDepthFormat,
            preferredMultiSampleCount);

        // Use the protected constructor that takes SurfaceType.SwapChainRenderTarget.
        // This skips the PlatformConstruct() call.
        var renderTarget = new RenderTarget2D(
            graphicsDevice,
            width,
            height,
            false,
            format,
            preferredDepthFormat,
            preferredMultiSampleCount,
            RenderTargetUsage.DiscardContents,
            SurfaceType.SwapChainRenderTarget);

        // Assign the handle to the native wrapper for the source image.
        // We set Owned = true because MonoGame still owns the MGG_Texture* wrapper, including its views and depth buffer,
        // and needs to manage its lifetime.
        // MGG_Texture_Destroy will skip the source swap-chain image.
        renderTarget.Handle = handle;
        renderTarget.Owned = true;

        return renderTarget;
    }
}


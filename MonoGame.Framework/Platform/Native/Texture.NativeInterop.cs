// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MonoGame.Interop;

namespace Microsoft.Xna.Framework.Graphics;

public abstract partial class Texture
{
    /// <summary>
    /// Gets the underlying native image handle for XR or external interop.
    /// Returns VkImage on Vulkan, ID3D12Resource* on DX12.
    /// </summary>
    public unsafe nint GetNativeImageHandle()
    {
        return MGG.Texture_GetNativeImage(Handle);
    }
}

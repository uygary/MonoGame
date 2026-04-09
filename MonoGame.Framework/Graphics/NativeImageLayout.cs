// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Backend-agnostic image layout/state for native interop operations.
/// Automatically maps to the correct backend-specific value
/// (VkImageLayout or D3D12_RESOURCE_STATES) in the native layer.
/// </summary>
public enum NativeImageLayout
{
    /// <summary>
    /// The image is currently used as a render target / color attachment.
    /// Vulkan: COLOR_ATTACHMENT_OPTIMAL. DX12: RENDER_TARGET.
    /// </summary>
    RenderTarget = 0,

    /// <summary>
    /// The image is currently bound for shader reading.
    /// Vulkan: SHADER_READ_ONLY_OPTIMAL. DX12: PIXEL_SHADER_RESOURCE.
    /// </summary>
    ShaderReadOnly = 1,
}

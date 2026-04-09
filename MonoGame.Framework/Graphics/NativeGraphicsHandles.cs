// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MonoGame.Framework.Utilities;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Contains native graphics API handles for XR and external interop.
/// <see cref="Device"/> and <see cref="Queue"/> are always populated.
/// Other fields depend on <see cref="Backend"/>.
/// </summary>
public struct NativeGraphicsHandles
{
    /// <summary>The active graphics backend.</summary>
    public GraphicsBackend Backend;

    /// <summary>Vulkan: VkInstance handle. Other backends: zero.</summary>
    public nint Instance;

    /// <summary>Vulkan: VkPhysicalDevice handle. Other backends: zero.</summary>
    public nint PhysicalDevice;

    /// <summary>
    /// The primary graphics device handle.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For Vulkan, this is <c>VkDevice</c>.
    /// </para>
    /// <para>
    /// For DX12:, this is <c>ID3D12Device*</c>.
    /// </para>
    /// </remarks>
    public nint Device;

    /// <summary>
    /// The primary graphics queue handle.
    /// Vulkan: VkQueue. DX12: ID3D12CommandQueue*.
    /// </summary>
    public nint Queue;

    /// <summary>Vulkan: queue family index. Other backends: 0.</summary>
    public int QueueFamilyIndex;

    /// <summary>Vulkan: queue index within family. Other backends: 0.</summary>
    public int QueueIndex;
}

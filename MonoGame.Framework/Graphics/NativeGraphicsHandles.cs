// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using MonoGame.Framework.Utilities;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Contains native graphics API handles for XR or other external interop.
/// <see cref="Device"/> and <see cref="Queue"/> are always populated.
/// Other fields depend on <see cref="Backend"/>.
/// </summary>
public readonly struct NativeGraphicsHandles
{
    /// <summary>
    /// The active graphics backend.
    /// </summary>
    public readonly GraphicsBackend Backend;

    /// <summary>
    /// Vulkan: <c>VkInstance</c> handle. Other backends: Zero.
    /// </summary>
    public readonly nint Instance;

    /// <summary>
    /// Vulkan: <c>VkPhysicalDevice</c> handle. Other backends: Zero.
    /// </summary>
    public readonly nint PhysicalDevice;

    /// <summary>
    /// The primary graphics device handle.
    /// </summary>
    /// <remarks>
    /// Only native backends are implemented.
    /// <para>
    /// For Vulkan, this is <c>VkDevice</c>.
    /// For DX12, this is <c>ID3D12Device*</c>.
    /// Other backends: Zero.
    /// </para>
    /// </remarks>
    public readonly nint Device;

    /// <summary>
    /// The primary graphics queue handle.
    /// Vulkan: <c>VkQueue</c>. DX12: <c>ID3D12CommandQueue*</c>.
    /// </summary>
    public readonly nint Queue;

    /// <summary>
    /// Vulkan: queue family index.
    /// Other backends: Zero.
    /// </summary>
    public readonly int QueueFamilyIndex;

    /// <summary>
    /// Vulkan: queue index within family.
    /// Other backends: Zero.
    /// </summary>
    public readonly int QueueIndex;

    /// <summary>
    /// Creates a new <see cref="NativeGraphicsHandles"/> instance that holds the provided handles (pointers).
    /// </summary>
    public NativeGraphicsHandles(
        GraphicsBackend backend,
        nint instance,
        nint physicalDevice,
        nint device,
        nint queue,
        int queueFamilyIndex,
        int queueIndex)
    {
        Backend = backend;
        Instance = instance;
        PhysicalDevice = physicalDevice;
        Device = device;
        Queue = queue;
        QueueFamilyIndex = queueFamilyIndex;
        QueueIndex = queueIndex;
    }
}

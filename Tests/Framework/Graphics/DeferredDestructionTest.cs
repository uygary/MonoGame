// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Interop;
using NUnit.Framework;
using System.Runtime.InteropServices;

namespace MonoGame.Tests.Graphics
{
    [TestFixture]
    [NonParallelizable]
    internal class DeferredDestructionTest
    {
#if VULKAN || DIRECTX12
        
        #region Native Helpers

        [DllImport("mgruntime", EntryPoint = "MGG_GraphicsDevice_GetPendingDestroyCount", ExactSpelling = true)]
        private static extern unsafe int GetPendingDestroyCount(MGG_GraphicsDevice* device);

        private unsafe int GetDestroyQueueSize(GraphicsDevice device)
        {
            return GetPendingDestroyCount(device.Handle);
        }

        /// <summary>
        /// USed to retrieve the frame field from a texture.
        /// MGG_Texture layout is different in Vulkan and DX12.
        /// </summary>
        /// <remarks>If either layout changes, this would need to be updtaed!</remarks>
        private unsafe int GetTextureFrame(Texture2D texture)
        {
            var textureHandle = (uint*)texture.Handle;
#if VULKAN
            return (int)textureHandle[1]; // skip writeFrame
#elif DIRECTX12
            return (int)textureHandle[0];
#else
            throw new NotImplementedException($"{nameof(GetTextureFrame)} is not implemented for this platform.");
#endif
        }

        #endregion Native Helpers

        [Test]
        [RunOnUI]
        public void DisposeAfterRenderTargetPresented()
        {
            var testGame = new TestGameBase()
            {
                IsFixedTimeStep = false,
            };
            new GraphicsDeviceManager(testGame)
            {
                GraphicsProfile = GraphicsProfile.HiDef,
            };
            var graphicsDeviceManager = testGame.Services.GetService<IGraphicsDeviceManager>();
            graphicsDeviceManager.CreateDevice();
            var graphicsDevice = testGame.GraphicsDevice;

            // Create RenderTargets to be put on deferred destruction queues.
            var renderTarget1 = new RenderTarget2D(graphicsDevice, 4096, 4096);
            var renderTaraget2 = new RenderTarget2D(
                graphicsDevice,
                4096,
                4096,
                false,
                SurfaceFormat.Color,
                DepthFormat.Depth24);

            // Issue GPU commands on renderTarget1.
            graphicsDevice.SetRenderTarget(renderTarget1);
            graphicsDevice.Clear(Color.Red);
            graphicsDevice.SetRenderTarget(null);
            graphicsDevice.Present();

            // Issue GPU commands on renderTaraget2.
            graphicsDevice.SetRenderTarget(renderTaraget2);
            graphicsDevice.Clear(Color.Blue);
            graphicsDevice.SetRenderTarget(null);
            graphicsDevice.Present();

            // Dispose resources and the game immediately.
            renderTarget1.Dispose();
            renderTaraget2.Dispose();
            testGame.Dispose();

            // We should only reach here if there were no out-of-order disposals of resources.
            Assert.Pass();
        }

        [Test]
        [RunOnUI]
        public void ResourceFreedAtHighFrameCount()
        {
            var testGame = new TestGameBase()
            {
                IsFixedTimeStep = false,
            };
            new GraphicsDeviceManager(testGame)
            {
                GraphicsProfile = GraphicsProfile.HiDef,
                SynchronizeWithVerticalRetrace = false,
            };
            
            var graphicsDeviceManager = testGame.Services.GetService<IGraphicsDeviceManager>();
            graphicsDeviceManager.CreateDevice();
            var graphicsDevice = testGame.GraphicsDevice;
            graphicsDevice.PresentationParameters.PresentationInterval = PresentInterval.Immediate;
            
            var renderTarget = new RenderTarget2D(graphicsDevice, 8, 8);
            
            graphicsDevice.SetRenderTarget(renderTarget);
            graphicsDevice.Clear(Color.Red);
            graphicsDevice.SetRenderTarget(null);
            graphicsDevice.Present();

            // TODO: I think the 0xFFFF logic is buggy.
            for (var i = 0; i < 65535; i++)
            {
                graphicsDevice.Clear(Color.Black);
                graphicsDevice.Present();
            }

            var baselineDestroyQueueSize = GetDestroyQueueSize(graphicsDevice);

            renderTarget.Dispose();
            var postDisposeDestroyQueueSize = GetDestroyQueueSize(graphicsDevice);
            
            Assert.AreEqual(baselineDestroyQueueSize + 1,
                postDisposeDestroyQueueSize,
                "RenderTarget should be in destroy queue.");

            graphicsDevice.Clear(Color.Black);
            graphicsDevice.Present();

            var postPresentDestroyQueueSize = GetDestroyQueueSize(graphicsDevice);

            Assert.AreEqual(baselineDestroyQueueSize,
                postPresentDestroyQueueSize,
                $"Resource with old age was not freed. Expected destroy queue size: {baselineDestroyQueueSize}, actual: {postPresentDestroyQueueSize}");

            renderTarget.Dispose();
            testGame.Dispose();
        }

        [Test]
        [RunOnUI]
        public void SetRenderTargetUpdatesTextureFrame()
        {
            var testGame = new TestGameBase()
            {
                IsFixedTimeStep = false,
            };
            new GraphicsDeviceManager(testGame)
            {
                GraphicsProfile = GraphicsProfile.HiDef,
                SynchronizeWithVerticalRetrace = false,
            };

            var graphicsDeviceManager = testGame.Services.GetService<IGraphicsDeviceManager>();
            graphicsDeviceManager.CreateDevice();
            var graphicsDevice = testGame.GraphicsDevice;
            graphicsDevice.PresentationParameters.PresentationInterval = PresentInterval.Immediate;
            
            var renderTarger = new RenderTarget2D(graphicsDevice, 64, 64);
            var frameZero = GetTextureFrame(renderTarger);

            // Advance 10 frames past creation.
            for (int i = 0; i < 10; i++)
            {
                graphicsDevice.Clear(Color.Black);
                graphicsDevice.Present();
            }

            var frameBeforeBind = GetTextureFrame(renderTarger);

            // Frame shouldn't bump unless render target's bound.
            Assert.AreEqual(frameZero,
                frameBeforeBind,
                $"Texture frame should not change in unbound {nameof(RenderTarget2D)}.");

            // Update texture framee.
            graphicsDevice.SetRenderTarget(renderTarger);
            graphicsDevice.Clear(Color.Red);
            var frameAfterBind = GetTextureFrame(renderTarger);

            graphicsDevice.SetRenderTarget(null);
            graphicsDevice.Present();

            // Frame should bump since render target was bound.
            Assert.Greater(frameAfterBind,
                frameBeforeBind,
                $"Texture frame should change in bound {nameof(RenderTarget2D)}.");

            renderTarger.Dispose();
        }

        [Test]
        [RunOnUI]
        public void RenderTargetDisposalAfterBindDefersDestruction()
        {
            var testGame = new TestGameBase()
            {
                IsFixedTimeStep = false,
            };
            new GraphicsDeviceManager(testGame)
            {
                GraphicsProfile = GraphicsProfile.HiDef,
                SynchronizeWithVerticalRetrace = false,
            };

            var graphicsDeviceManager = testGame.Services.GetService<IGraphicsDeviceManager>();
            graphicsDeviceManager.CreateDevice();
            var graphicsDevice = testGame.GraphicsDevice;
            graphicsDevice.PresentationParameters.PresentationInterval = PresentInterval.Immediate;
            
            var renderTarget = new RenderTarget2D(
                graphicsDevice,
                64,
                64,
                false,
                SurfaceFormat.Color,
                DepthFormat.Depth24Stencil8);

            // Go past kFreeFrames threshold.
            for (var i = 0; i < 10; i++)
            {
                graphicsDevice.Clear(Color.Black);
                graphicsDevice.Present();
            }

            graphicsDevice.SetRenderTarget(renderTarget);
            graphicsDevice.Clear(Color.Red);
            graphicsDevice.SetRenderTarget(null);

            var baselineDsetroyQueueSize = GetDestroyQueueSize(graphicsDevice);

            renderTarget.Dispose();
            var destroyQueueSizeAfterDisposal = GetDestroyQueueSize(graphicsDevice);

            Assert.Greater(destroyQueueSizeAfterDisposal,
                baselineDsetroyQueueSize,
                $"Disposed {nameof(RenderTarget2D)} should enter destroy queue.");

            graphicsDevice.Clear(Color.Black);
            graphicsDevice.Present();

            // This actually seems to crash, but still, the test is useful.
            var destroyQueueSizeAfterPresentation = GetDestroyQueueSize(graphicsDevice);

            Assert.AreEqual(destroyQueueSizeAfterDisposal,
                destroyQueueSizeAfterPresentation,
                "underlying texture was prematurely destroyed.");
        }

#endif
    }
}

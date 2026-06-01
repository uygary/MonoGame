// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

#if VULKAN || DIRECTX12

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Interop;
using NUnit.Framework;
using System.Runtime.InteropServices;

namespace MonoGame.Tests.Graphics
{
    [TestFixture]
    [Category("MemoryLifeCycle")]
    [NonParallelizable]
    internal class DeferredDestructionTest
    {
        
        #region Native Helpers

        [DllImport("mgruntime", EntryPoint = "MGG_GraphicsDevice_GetDestroyQueueSize", ExactSpelling = true)]
        private static extern unsafe int GraphicsDevice_GetDestroyQueueSize(MGG_GraphicsDevice* device);

        [DllImport("mgruntime", EntryPoint = "MGG_GraphicsDevice_GetCurrentFrame", ExactSpelling = true)]
        private static extern unsafe int GraphicsDevice_GetCurrentFrame(MGG_GraphicsDevice* device);

        [DllImport("mgruntime", EntryPoint = "MGG_GraphicsDevice_GetFreeFrames", ExactSpelling = true)]
        private static extern unsafe int GraphicsDevice_GetFreeFrames(MGG_GraphicsDevice* device);

        private unsafe int GetDestroyQueueSize(GraphicsDevice device)
        {
            return GraphicsDevice_GetDestroyQueueSize(device.Handle);
        }

        private unsafe int GetCurrentFrame(GraphicsDevice device)
        {
            return GraphicsDevice_GetCurrentFrame(device.Handle);
        }

        private unsafe int GetFreeFrames(GraphicsDevice device)
        {
            return GraphicsDevice_GetFreeFrames(device.Handle);
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
        public void WaitForGpuToIdleBeforeDisposingBuffer()
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

            // Repeat many times to catch the racing condition.
            for (var i = 0; i < 256; i++)
            {
                // Create a vertex buffer.
                var vertexBuffer = new VertexBuffer(
                    graphicsDevice,
                    VertexPositionColor.VertexDeclaration,
                    3,
                    BufferUsage.None);

                // Issue GPU commands referencing the buffer.
                vertexBuffer.SetData(
                [
                    new VertexPositionColor(new Vector3(1, 0, 0), Color.Red),
                    new VertexPositionColor(new Vector3(0, 1, 0), Color.Green),
                    new VertexPositionColor(new Vector3(0, 0, 1), Color.Blue),
                ]);

                // Create index buffer.
                var indexBuffer = new IndexBuffer(
                    graphicsDevice,
                    IndexElementSize.SixteenBits,
                    3,
                    BufferUsage.None);

                // Issue GPU commands referencing the buffer.
                indexBuffer.SetData(new short[] { 0, 1, 2 });

                graphicsDevice.Clear(Color.Black);
                graphicsDevice.Present();

                // Dispose while GPU *might* still be referencing the buffer memory.
                vertexBuffer.Dispose();
                indexBuffer.Dispose();
            }

            //graphicsDevice.Dispose();
            testGame.Dispose();

            Assert.Pass();
        }

        [Test]
        [RunOnUI]
        public void WaitForGpuToIdleBeforeDisposingRenderTarget()
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

            for (var i = 0; i < 100; i++)
            {
                var rt = new RenderTarget2D(graphicsDevice, 64, 64);

                graphicsDevice.SetRenderTarget(rt);
                graphicsDevice.Clear(new Color(i * 5, 0, 0));
                graphicsDevice.SetRenderTarget(null);
                graphicsDevice.Present();

                rt.Dispose();
            }

            //graphicsDevice.Dispose();
            testGame.Dispose();

            // We should only get here if we correctly wait for the GPU on each disposal.
            Assert.Pass();
        }

        [Test]
        [RunOnUI]
        public void RenderTargetTextureSurvivesDeferredDestruction()
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

            var renderTarget = new RenderTarget2D(graphicsDevice, 128, 128);

            // Advance frames past threshold to make texture appear stale.
            for (var i = 0; i < 10; i++)
            {
                graphicsDevice.Clear(Color.Black);
                graphicsDevice.Present();
            }

            // Bind RenderTarget and present.
            graphicsDevice.SetRenderTarget(renderTarget);
            graphicsDevice.Clear(Color.MonoGameOrange);
            graphicsDevice.SetRenderTarget(null);
            graphicsDevice.Present();

            // Read RenderTarget texture to verify the texture frame was updated.
            var data = new Color[128 * 128];
            renderTarget.GetData(data);

            Assert.AreEqual(
                Color.MonoGameOrange,
                data[0],
                "Render target texture was destroyed prematurely.");

            renderTarget.Dispose();
            //graphicsDevice.Dispose();
            testGame.Dispose();
        }

        [Test]
        [RunOnUI]
        public void RenderTargetTextureWithDepthSurvivesDeferredDestruction()
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

            var renderTarget = new RenderTarget2D(
                graphicsDevice,
                128,
                128,
                false,
                SurfaceFormat.Color,
                DepthFormat.Depth24);

            // Advance frames past threshold to make texture appear stale.
            for (var i = 0; i < 10; i++)
            {
                graphicsDevice.Clear(Color.Black);
                graphicsDevice.Present();
            }

            // Bind RenderTarget and present.
            graphicsDevice.SetRenderTarget(renderTarget);
            graphicsDevice.Clear(Color.MonoGameOrange);
            graphicsDevice.SetRenderTarget(null);
            graphicsDevice.Present();

            // Read RenderTarget texture to verify the texture frame was updated.
            var data = new Color[128 * 128];
            renderTarget.GetData(data);

            Assert.AreEqual(
                Color.MonoGameOrange,
                data[0],
                "Render target texture with depth was destroyed prematurely.");

            renderTarget.Dispose();
            //graphicsDevice.Dispose();
            testGame.Dispose();
        }

        [Test]
        [RunOnUI]
        public void MultipleRenderTargetsDisposeOrder()
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

            var renderTarget1 = new RenderTarget2D(graphicsDevice, 128, 128);
            var renderTarget2 = new RenderTarget2D(graphicsDevice, 128, 128);
            var renderTarget3 = new RenderTarget2D(graphicsDevice, 128, 128);
            var renderTarget4 = new RenderTarget2D(graphicsDevice, 128, 128);

            // Bind and use each.
            graphicsDevice.SetRenderTarget(renderTarget1);
            graphicsDevice.Clear(Color.Red);

            graphicsDevice.SetRenderTarget(renderTarget2);
            graphicsDevice.Clear(Color.Green);

            graphicsDevice.SetRenderTarget(renderTarget3);
            graphicsDevice.Clear(Color.Blue);

            graphicsDevice.SetRenderTarget(renderTarget4);
            graphicsDevice.Clear(Color.MonoGameOrange);

            graphicsDevice.SetRenderTarget(null);
            graphicsDevice.Present();

            renderTarget4.Dispose();
            renderTarget3.Dispose();
            renderTarget2.Dispose();
            renderTarget1.Dispose();
            //graphicsDevice.Dispose();
            testGame.Dispose();

            Assert.Pass();
        }

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
            //graphicsDevice.Dispose();
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
            var textureFrame = GetTextureFrame(renderTarget);
            var frameBefore = GetCurrentFrame(graphicsDevice);
            var freeFrames = GetFreeFrames(graphicsDevice);

            renderTarget.Dispose();
            var postDisposeDestroyQueueSize = GetDestroyQueueSize(graphicsDevice);
            
            Assert.AreEqual(baselineDestroyQueueSize + 1,
                postDisposeDestroyQueueSize,
                "RenderTarget should be in destroy queue.");

            for (var i = 0; i < freeFrames + 1; i++)
            {
                graphicsDevice.Clear(Color.Black);
                graphicsDevice.Present();
            }

            var postPresentDestroyQueueSize = GetDestroyQueueSize(graphicsDevice);
            var postPresentationFrame = GetCurrentFrame(graphicsDevice);

            Assert.AreEqual(baselineDestroyQueueSize,
                postPresentDestroyQueueSize,
                $"""
                 Resource with old age was not freed.
                 Expected destroy queue size: {baselineDestroyQueueSize}
                 Actual destroy queue size: {postPresentDestroyQueueSize}
                 Texture frame: {textureFrame}
                 Frame before disposal: {frameBefore}
                 Frame after presentation: {postPresentationFrame}
                 Free frames: {freeFrames}
                 """);

            renderTarget.Dispose();
            //graphicsDevice.Dispose();
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
            //graphicsDevice.Dispose();
            testGame.Dispose();
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

            renderTarget.Dispose();
            //graphicsDevice.Dispose();
            testGame.Dispose();
        }
    }
}

#endif

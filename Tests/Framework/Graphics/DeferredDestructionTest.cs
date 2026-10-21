// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

#if VULKAN || DIRECTX12

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NUnit.Framework;

namespace MonoGame.Tests.Graphics
{
    [Category("MemoryLifeCycle")]
    [NonParallelizable]
    [RunOnUiTestFixture]
    internal class DeferredDestructionTest
    {
        [Test]
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
            var renderTarget2 = new RenderTarget2D(
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

            // Issue GPU commands on renderTarget2.
            graphicsDevice.SetRenderTarget(renderTarget2);
            graphicsDevice.Clear(Color.Blue);
            graphicsDevice.SetRenderTarget(null);
            graphicsDevice.Present();

            // Dispose resources and the game immediately.
            renderTarget1.Dispose();
            renderTarget2.Dispose();
            //graphicsDevice.Dispose();
            testGame.Dispose();

            // We should only reach here if there were no out-of-order disposals of resources.
            Assert.Pass();
        }

#if VULKAN
        [Test]
        public void FinalizersRunDuringGraphicsDeviceDispose()
        {
            var testGame = CreateTestGame();
            var graphicsDevice = testGame.GraphicsDevice;

            graphicsDevice.Clear(Color.Black);
            graphicsDevice.Present();

            // The gate is allocated first so the finalizer thread reaches it before the buffers.
            // It stalls the finalizer thread, which keeps the buffer finalizers pending while the device is disposed.
            // The device must wait for them before destroying the native device,
            // and finalizers that see a disposed device must not touch it.
            AllocateFinalizerGate();
            AllocateUnreferencedBuffers(graphicsDevice, 256);

            GC.Collect();
            testGame.Dispose();

            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        [Test]
        public void DisposedSecondaryVertexStreamIsNotBoundAfterDestruction()
        {
            var testGame = CreateTestGame();
            var graphicsDevice = testGame.GraphicsDevice;

            var positionDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0));
            var colorDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Color, VertexElementUsage.Color, 0));

            var indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, 3, BufferUsage.WriteOnly);
            indexBuffer.SetData(new short[] { 0, 1, 2 });
            var singleStream = new VertexBuffer(graphicsDevice, typeof(VertexPositionColor), 3, BufferUsage.WriteOnly);
            singleStream.SetData(CreateTriangleVertices());
            var effect = new BasicEffect(graphicsDevice) { VertexColorEnabled = true };

            for (var iteration = 0; iteration < 4; iteration++)
            {
                var positions = new VertexBuffer(graphicsDevice, positionDeclaration, 3, BufferUsage.WriteOnly);
                positions.SetData(new[] { new Vector3(0f, 0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, -0.5f, 0f) });
                var colors = new VertexBuffer(graphicsDevice, colorDeclaration, 3, BufferUsage.WriteOnly);
                colors.SetData(new[] { Color.Red, Color.Green, Color.Blue });

                // Draw with two vertex streams so the native device binds slot 1.
                graphicsDevice.SetVertexBuffers(new VertexBufferBinding(positions), new VertexBufferBinding(colors));
                graphicsDevice.Indices = indexBuffer;
                graphicsDevice.Clear(Color.Black);
                ApplyAndDraw(graphicsDevice, effect);
                graphicsDevice.Present();

                positions.Dispose();
                colors.Dispose();

                // Present without drawing so the disposed streams age past the deferred destruction window and are actually freed.
                PresentEmptyFrames(graphicsDevice, 8);

                // Draw with a single stream. Slot 1 still points at the freed buffer unless the device scrubs it and only binds active streams.
                DrawTriangle(graphicsDevice, effect, singleStream, indexBuffer);
                graphicsDevice.Present();
            }

            effect.Dispose();
            singleStream.Dispose();
            indexBuffer.Dispose();
            testGame.Dispose();
        }

        [Test]
        public void ConcurrentResourceCreationDiscardFinalizationAndSwapchainReset()
        {
            // A lock-ordering regression shows up as a hang in this test rather than a failure.
            var testGame = CreateTestGame();
            var graphicsDevice = testGame.GraphicsDevice;

            var vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionColor), 3, BufferUsage.WriteOnly);
            vertexBuffer.SetData(CreateTriangleVertices());
            var indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, 3, BufferUsage.WriteOnly);
            indexBuffer.SetData(new short[] { 0, 1, 2 });
            var effect = new BasicEffect(graphicsDevice) { VertexColorEnabled = true };

            Exception backgroundException = null;
            var stopWorkers = false;
            var workers = new List<Thread>();

            for (var t = 0; t < 4; t++)
            {
                var worker = new Thread(() =>
                {
                    try
                    {
                        var pixels = new Color[16 * 16];
                        var readback = new Color[16 * 16];
                        var dynamicVertices = new VertexPositionColor[12];
                        var dynamicIndices = new short[] { 0, 1, 2, 1, 2, 3 };

                        while (!Volatile.Read(ref stopWorkers))
                        {
                            using (var texture = new Texture2D(graphicsDevice, 16, 16))
                            {
                                texture.SetData(pixels);
                                texture.GetData(readback);
                            }

                            using (var dynamicVertexBuffer = new DynamicVertexBuffer(graphicsDevice, typeof(VertexPositionColor), dynamicVertices.Length, BufferUsage.WriteOnly))
                            {
                                dynamicVertexBuffer.SetData(dynamicVertices, 0, dynamicVertices.Length, SetDataOptions.Discard);
                                dynamicVertexBuffer.SetData(dynamicVertices, 0, dynamicVertices.Length, SetDataOptions.Discard);
                            }

                            using (var dynamicIndexBuffer = new DynamicIndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, dynamicIndices.Length, BufferUsage.WriteOnly))
                            {
                                dynamicIndexBuffer.SetData(dynamicIndices, 0, dynamicIndices.Length, SetDataOptions.Discard);
                            }

                            AllocateUnreferencedResources(graphicsDevice);
                            GC.Collect();
                        }
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref backgroundException, ex, null);
                    }
                })
                {
                    IsBackground = true,
                };
                workers.Add(worker);
                worker.Start();
            }

            var stopwatch = Stopwatch.StartNew();
            for (var frame = 0; frame < 300 && stopwatch.Elapsed < TimeSpan.FromSeconds(20); frame++)
            {
                if (frame % 50 == 25)
                {
                    graphicsDevice.PresentationParameters.BackBufferWidth = 128 + frame;
                    graphicsDevice.PresentationParameters.BackBufferHeight = 128 + frame;
                    graphicsDevice.PresentationParameters.PresentationInterval =
                        (frame % 100 == 25) ? PresentInterval.One : PresentInterval.Immediate;
                    graphicsDevice.Reset();
                }

                // Destroying a texture on the render thread every frame makes Present()
                // drain the texture destruction queue while the workers upload textures.
                var transient = new Texture2D(graphicsDevice, 4, 4);
                transient.SetData(new Color[16]);

                DrawTriangle(graphicsDevice, effect, vertexBuffer, indexBuffer);
                graphicsDevice.Present();
                transient.Dispose();
            }

            Volatile.Write(ref stopWorkers, true);
            foreach (var worker in workers)
            {
                Assert.True(worker.Join(TimeSpan.FromSeconds(10)), "Background worker timed out.");
            }

            Assert.IsNull(backgroundException, $"Background worker threw an exception: {backgroundException}");

            effect.Dispose();
            vertexBuffer.Dispose();
            indexBuffer.Dispose();
            testGame.Dispose();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private static TestGameBase CreateTestGame()
        {
            var testGame = new TestGameBase()
            {
                IsFixedTimeStep = false,
            };
            new GraphicsDeviceManager(testGame)
            {
                GraphicsProfile = GraphicsProfile.HiDef,
                PreferredBackBufferWidth = 128,
                PreferredBackBufferHeight = 128,
                SynchronizeWithVerticalRetrace = false,
            };
            testGame.Services.GetService<IGraphicsDeviceManager>().CreateDevice();
            return testGame;
        }

        private static VertexPositionColor[] CreateTriangleVertices()
        {
            return new[]
            {
                new VertexPositionColor(new Vector3(0f, 0.5f, 0f), Color.Red),
                new VertexPositionColor(new Vector3(0.5f, -0.5f, 0f), Color.Green),
                new VertexPositionColor(new Vector3(-0.5f, -0.5f, 0f), Color.Blue),
            };
        }

        private static void ApplyAndDraw(GraphicsDevice graphicsDevice, Effect effect)
        {
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 1);
            }
        }

        private static void DrawTriangle(GraphicsDevice graphicsDevice, Effect effect, VertexBuffer vertexBuffer, IndexBuffer indexBuffer)
        {
            graphicsDevice.SetVertexBuffer(vertexBuffer);
            graphicsDevice.Indices = indexBuffer;
            graphicsDevice.Clear(Color.Black);
            ApplyAndDraw(graphicsDevice, effect);
        }

        private static void PresentEmptyFrames(GraphicsDevice graphicsDevice, int count)
        {
            for (var i = 0; i < count; i++)
            {
                graphicsDevice.Clear(Color.Black);
                graphicsDevice.Present();
            }
        }

        private sealed class FinalizerGate
        {
            ~FinalizerGate()
            {
                Thread.Sleep(500);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AllocateFinalizerGate()
        {
            _ = new FinalizerGate();
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AllocateUnreferencedBuffers(GraphicsDevice graphicsDevice, int count)
        {
            var vertices = CreateTriangleVertices();
            for (var i = 0; i < count; i++)
            {
                var vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionColor), 3, BufferUsage.WriteOnly);
                vertexBuffer.SetData(vertices);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AllocateUnreferencedResources(GraphicsDevice graphicsDevice)
        {
            var pixels = new Color[8 * 8];
            var vertices = new VertexPositionColor[6];
            var indices = new short[] { 0, 1, 2, 1, 2, 3 };

            for (var i = 0; i < 4; i++)
            {
                var texture = new Texture2D(graphicsDevice, 8, 8);
                texture.SetData(pixels);

                var renderTarget = new RenderTarget2D(graphicsDevice, 8, 8, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);

                var vertexBuffer = new DynamicVertexBuffer(graphicsDevice, typeof(VertexPositionColor), vertices.Length, BufferUsage.WriteOnly);
                vertexBuffer.SetData(vertices, 0, vertices.Length, SetDataOptions.Discard);

                var indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
                indexBuffer.SetData(indices);

                // Each effect clone owns its own constant buffers.
                var effect = new BasicEffect(graphicsDevice);
            }
        }
#endif
    }
}

#endif

// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NUnit.Framework;

namespace MonoGame.Tests.Graphics
{
    [TestFixture]
    [NonParallelizable]
#if DESKTOPGL
    [Ignore("GL doesn't work well with threads.")]
#endif
    class Texture2DThreadingTests : GraphicsDeviceTestFixtureBase
    {
        [Test]
        public void CreateSetAndGetData()
        {
            const int Width = 32;
            const int Height = 32;
            var fillColor = Color.MonoGameOrange;

            Exception threadException = null;
            Color[] readBack = null;

            var thread = new Thread(() =>
            {
                try
                {
                    using var texture = new Texture2D(gd, Width, Height);

                    var pixels = new Color[Width * Height];
                    for (int i = 0; i < pixels.Length; i++)
                        pixels[i] = fillColor;

                    texture.SetData(pixels);

                    readBack = new Color[Width * Width];
                    texture.GetData(readBack);
                }
                catch (Exception ex)
                {
                    threadException = ex;
                }
            });

            thread.Start();

            // Wait for 10 seconds for it to finish.
            var finished = thread.Join(TimeSpan.FromSeconds(10));

            Assert.True(finished, "Threaded SetData/GetData failed!");
            Assert.IsNull(threadException, $"Thread threw an exception: {threadException}");
            Assert.NotNull(readBack, "GetData failed!");            
            Assert.True(readBack.All(c => c == Color.MonoGameOrange), "Read color data does not match!");
        }

        [Test]
        public void BackgroundLoading()
        {
            var textures = new string[]
            {
                "Assets/Textures/LogoOnly_64px.gif",
                "Assets/Textures/LogoOnly_64px.jpg",
                "Assets/Textures/LogoOnly_64px.png",
                "Assets/Textures/1bit.png",
                "Assets/Textures/8bit.png",
                "Assets/Textures/24bit.png",
                "Assets/Textures/32bit.png",
                "Assets/Textures/sample_1280x853.hdr"
            };
            const int COUNT = 100;

            var loaded = new List<Texture2D>();

            Exception threadException = null;

            var thread = new Thread(() =>
            {
                Thread.Sleep(10);

                try
                {
                    int count = COUNT;
                    while (--count > 0)
                    {
                        foreach (var texture in textures)
                            loaded.Add(Texture2D.FromFile(gd, texture));
                    }
                }
                catch (Exception ex)
                {
                    threadException = ex;
                }
            });

            thread.Start();

            int frames = 0;

            while (frames < 1000)
            {
                gd.Clear(Color.MonoGameOrange);
                gd.Present();
                ++frames;

                if (!thread.IsAlive)
                    break;
            }

            Assert.True(frames > 1, "Should have presented more than once!");
            Assert.AreEqual(textures.Length * COUNT, loaded.Count, "Some textures failed to load!");
        }
    }
}

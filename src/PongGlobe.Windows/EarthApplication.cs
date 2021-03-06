﻿using AssetPrimitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Veldrid;
using ImGuiNET;
using PongGlobe.Core;
using PongGlobe.Scene;
using PongGlobe.Renders;
using System.Text;
using Xilium.CefGlue;
using System.Collections.Concurrent;

namespace PongGlobe.Windows
{
    //地球的应用程序对象
    public  class EarthApplication
    {
        private CommandList _cl;

        /// <summary>
        /// 当前地球的场景对象
        /// </summary>
        protected PongGlobe.Scene.Scene _scene;
        /// <summary>
        /// 当前的各个子渲染对象
        /// </summary>
        private List<IRender> renders = new List<IRender>();
        //地球渲染对象
        private IRender globeRender;
        private IRender vectorLayerRender;
        public ApplicationWindow Window { get; }
        public GraphicsDevice GraphicsDevice { get; private set; }
        public ResourceFactory ResourceFactory { get; private set; }
        public Swapchain MainSwapchain { get; private set; }
        private MainUIRender _browHost;
        private float _ticks;
        protected ImGuiRenderer _controller = null;
        protected static FrameTimeAverager _fta = new FrameTimeAverager(0.666);
        //主程序中的更新队列
        public readonly  ConcurrentQueue<IRender> _updateQuere = new ConcurrentQueue<IRender>();

        public EarthApplication(ApplicationWindow window)
        {
            Window = window;
            Window.Resized += HandleWindowResize;
            Window.GraphicsDeviceCreated += OnGraphicsDeviceCreated;
            Window.GraphicsDeviceDestroyed += OnDeviceDestroyed;
            Window.Rendering += PreDraw;
            Window.Rendering += Draw;            
            //首先创建一个场景对象
            _scene = new Scene.Scene(window.Width, window.Height);
            globeRender = new RayCastedGlobe(_scene);
            var path = @"E:\swyy\Lib\PongGlobe\PongGlobe\assets\Vector\NaturalEarth\110m-admin-0-countries\110m_admin_0_countries.shp";
            vectorLayerRender = new Renders.VectorLayerRender(path, _scene);
            var shareRender = new ShareRender(_scene);
            renders.Add(shareRender);
            renders.Add(globeRender);
            renders.Add(vectorLayerRender);
            var pat2h = @"E:\swyy\Lib\PongGlobe\PongGlobe\assets\Vector\NaturalEarth\110m-populated-places-simple\110m_populated_places_simple.shp";
            var vectorPoint = new PointVectorLayerRender(pat2h, _scene);
            //renders.Add(vectorPoint);
            //var drawline = new DrawLineTool(_scene);
            // renders.Add(drawline);
            

        }

        /// <summary>
        /// 在设备创建完成后初始化和创建资源
        /// </summary>
        /// <param name="gd"></param>
        /// <param name="factory"></param>
        /// <param name="sc"></param>
        public void OnGraphicsDeviceCreated(GraphicsDevice gd, ResourceFactory factory, Swapchain sc)
        {
            //临时创建UI
            var mainUi = new MainUIRender(gd, Window.Handle,Window.Width, Window.Height);
            renders.Add(mainUi);
            _browHost = mainUi;
            GraphicsDevice = gd;
            ResourceFactory = factory;
            MainSwapchain = sc;
            CreateResources(factory);
            CreateSwapchainResources(factory);            
            _controller = new ImGuiRenderer(this.GraphicsDevice, this.GraphicsDevice.MainSwapchain.Framebuffer.OutputDescription, (int)this.Window.Width, (int)this.Window.Height);
            
        }

        protected virtual void OnDeviceDestroyed()
        {
            GraphicsDevice = null;
            ResourceFactory = null;
            MainSwapchain = null;
        }

        protected virtual string GetTitle() => GetType().Name;

        protected virtual unsafe void CreateResources(ResourceFactory factory)
        {
            //创建一个公共资源
            _cl = factory.CreateCommandList();
            foreach (var item in renders)
            {
                item.CreateDeviceResources(GraphicsDevice, factory);
            }
        }

        protected virtual void CreateSwapchainResources(ResourceFactory factory) { }

        /// <summary>
        /// 渲染之前更新相关参数或者数据
        /// </summary>
        /// <param name="deltaSeconds"></param>
        protected virtual void PreDraw(float deltaSeconds)
        {            
            _controller.Update(1f / 60f, InputTracker.FrameSnapshot);
            //更新相机
            _scene.Camera.Update(deltaSeconds);
            _fta.AddTime(deltaSeconds);
            SubmitUI();
                      
            _ticks += deltaSeconds * 1000f;
            foreach (var item in renders)
            {
                item.Update();
            }
            //更新队列中的操作
            foreach (var item in _updateQuere)
            {
                item.Update();
            }
        }

        //显示imgui
        private  void SubmitUI()
        {
            {
                //显示帧率     
                ImGuiNET.ImGui.Text(_fta.CurrentAverageFramesPerSecond.ToString("000.0 fps / ") + _fta.CurrentAverageFrameTimeMilliseconds.ToString("#00.00 ms"));
                var bytes= "你好";
                ImGuiNET.ImGui.Text(bytes);
                if (ImGuiNET.ImGui.Button("Export Sc"))
                {
                    //_controller.SaveFontText();
                }
            }
        }

      

      



        /// <summary>
        /// 目前暂时先使用单个CommandList，在单线程的情况下提交渲染对象
        /// </summary>
        /// <param name="deltaSeconds"></param>
        protected virtual void Draw(float deltaSeconds)
        {
            _cl.Begin();
            _cl.SetFramebuffer(GraphicsDevice.MainSwapchain.Framebuffer);
            _cl.ClearColorTarget(0, RgbaFloat.Black);
            _cl.ClearDepthStencil(1f);
            foreach (var item in renders)
            {
                if (item is RayCastedGlobe) continue;
                item.Draw(_cl);
            }
            //最后渲染IMGUI
            _controller.Render(GraphicsDevice,_cl);           
            _cl.End();            
            GraphicsDevice.SubmitCommands(_cl);                                 
            GraphicsDevice.SwapBuffers(GraphicsDevice.MainSwapchain);
            GraphicsDevice.WaitForIdle();

            
        }
        /// <summary>
        /// 窗口发生变化时停止给场景的Camera对象
        /// </summary>
        protected virtual void HandleWindowResize()
        {
            _scene.Camera.WindowResized(Window.Width, Window.Height);
            _controller.WindowResized((int)Window.Width, (int)Window.Height);
            //调整UI界面的大小
            _browHost.ResizeWindow(Window.Width, Window.Height);
        }  
    }
}

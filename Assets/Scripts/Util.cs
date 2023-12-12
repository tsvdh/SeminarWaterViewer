using System;

namespace Configs
{
    [Serializable]
    public struct SimConfig
    {
        public int fps;
        public int seconds;
        public bool separateFiles;
        public int width;
        public int height;
    }

    [Serializable]
    public struct GlobalConfig
    {
        public string name;
    }
    
    [Serializable]
    public struct BuilderConfig
    {
        public BuilderUnit[] wall;
    }
    
    [Serializable]
    public struct BuilderUnit
    {
        public int[] topLeft;
        public int[] bottomRight;
        public int height;
    }
}
using System;
using UnityEngine;

namespace LemonFramework.UProfiler.Core
{
    public abstract class Singleton<T> where T : class, new()
    {
        public abstract class Options
        {
        }

        private static T _instance;

        public static T GetInstance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = Activator.CreateInstance<T>();
            return _instance;
        }

        public virtual void Initialize(Options options = null)
        {
        }

        public virtual void Dispose()
        {
            _instance = null;
        }
    }

    public abstract class MonoSingleton<T> : MonoBehaviour where T : Component
    {
        public abstract class Options
        {
        }

        private static T _instance;

        public static T GetInstance(string objName, GameObject obj = null)
        {
            if (_instance != null) return _instance;
            if (obj == null)
            {
                obj = new GameObject("[" + objName + "]");
            }

            _instance = (T) obj.AddComponent(typeof(T));
            return _instance;
        }

        public virtual void Initialize(Options options = null)
        {
        }

        public virtual void Dispose()
        {
            _instance = null;
            Destroy(gameObject);
        }
    }
}
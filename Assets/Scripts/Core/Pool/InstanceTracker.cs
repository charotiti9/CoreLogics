using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Pool
{
    /// <summary>
    /// 인스턴스 추적 정보 관리
    /// Address, Component Type, Target Parent 정보를 저장하고 조회합니다.
    /// </summary>
    /// <typeparam name="T">Component 타입</typeparam>
    public class InstanceTracker<T> where T : Component
    {
        /// <summary>
        /// 추적 정보 (Address, Component Type, Target Parent)
        /// </summary>
        private class TrackingInfo
        {
            public string Address;
            public Type ComponentType;
            public Transform TargetParent;

            public TrackingInfo(string address, Type componentType, Transform targetParent)
            {
                Address = address;
                ComponentType = componentType;
                TargetParent = targetParent;
            }
        }

        // 인스턴스 → 추적 정보 매핑
        private readonly Dictionary<T, TrackingInfo> tracking = new Dictionary<T, TrackingInfo>();

        /// <summary>
        /// 인스턴스 추적을 시작합니다.
        /// </summary>
        /// <param name="component">추적할 Component</param>
        /// <param name="address">리소스 Address</param>
        /// <param name="componentType">Component 타입</param>
        /// <param name="targetParent">Target Parent</param>
        public void Track(T component, string address, Type componentType, Transform targetParent)
        {
            tracking[component] = new TrackingInfo(address, componentType, targetParent);
        }

        /// <summary>
        /// 인스턴스 추적을 해제합니다.
        /// </summary>
        /// <param name="component">추적 해제할 Component</param>
        public void Untrack(T component)
        {
            tracking.Remove(component);
        }

        /// <summary>
        /// 인스턴스의 Address를 조회합니다.
        /// </summary>
        /// <param name="component">조회할 Component</param>
        /// <param name="address">조회된 Address</param>
        /// <returns>추적 중인 인스턴스면 true</returns>
        public bool TryGetAddress(T component, out string address)
        {
            if (tracking.TryGetValue(component, out var info))
            {
                address = info.Address;
                return true;
            }

            address = null;
            return false;
        }

        /// <summary>
        /// 인스턴스의 추적 정보를 조회합니다 (Address, Type, TargetParent 모두).
        /// </summary>
        /// <param name="component">조회할 Component</param>
        /// <param name="address">조회된 Address</param>
        /// <param name="componentType">조회된 Component 타입</param>
        /// <param name="targetParent">조회된 Target Parent</param>
        /// <returns>추적 중인 인스턴스면 true</returns>
        public bool TryGetInfo(T component, out string address, out Type componentType, out Transform targetParent)
        {
            if (tracking.TryGetValue(component, out var info))
            {
                address = info.Address;
                componentType = info.ComponentType;
                targetParent = info.TargetParent;
                return true;
            }

            address = null;
            componentType = null;
            targetParent = null;
            return false;
        }

        /// <summary>
        /// 추적 중인 모든 인스턴스를 반환합니다.
        /// </summary>
        public List<(T component, string address)> GetAll()
        {
            var result = new List<(T, string)>();

            foreach (var kvp in tracking)
            {
                result.Add((kvp.Key, kvp.Value.Address));
            }

            return result;
        }

        /// <summary>
        /// 추적 정보를 정리합니다.
        /// </summary>
        public void Clear()
        {
            tracking.Clear();
        }

        /// <summary>
        /// 현재 추적 중인 인스턴스 개수를 반환합니다.
        /// </summary>
        public int Count => tracking.Count;
    }
}

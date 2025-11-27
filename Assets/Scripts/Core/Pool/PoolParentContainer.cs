using UnityEngine;

namespace Core.Pool
{
    /// <summary>
    /// Pool Parent들을 관리하는 컨테이너
    /// 씬에 배치하여 FindObjectsByType 호출을 방지합니다.
    /// 없으면 PoolManager가 자동으로 생성합니다.
    /// </summary>
    public class PoolParentContainer : MonoBehaviour
    {
        private void Awake()
        {
            // DontDestroyOnLoad 설정
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 이름으로 자식 Transform을 찾습니다.
        /// </summary>
        /// <param name="parentName">찾을 Parent 이름</param>
        /// <returns>찾은 Transform, 없으면 null</returns>
        public Transform FindParent(string parentName)
        {
            return transform.Find(parentName);
        }

        /// <summary>
        /// 새 Parent를 자식으로 생성합니다.
        /// </summary>
        /// <param name="parentName">생성할 Parent 이름</param>
        /// <returns>생성된 Transform</returns>
        public Transform CreateParent(string parentName)
        {
            GameObject parentObj = new GameObject($"[Pool] {parentName}");
            parentObj.transform.SetParent(transform);
            return parentObj.transform;
        }
    }
}

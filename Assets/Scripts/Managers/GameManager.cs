using Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public class GameManager : MonoBehaviour
    {
        //Editor
        [SerializeField] private Transform canvas;
        [SerializeField] private GameObject objectHandler;
        [SerializeField] private GameObject objectPrefab;
        [SerializeField] private int levelSize = 50;
        [SerializeField] private int maxTargets = 5;
        [SerializeField] private float spawnTopHeightOffset = 100f;
        [SerializeField] private float spawnDelay;
        [SerializeField] private float handlersSpeed = 1f;
        [SerializeField] private int startFrom = 0;
        [SerializeField] private List<Sprite> spriteList;

        //Public Static
        public static GameManager Instance { get; private set; }
        public static Action<int, int> OnProgressUpdate;    //<CurrentHandlerIndex, HandlersCount>

        //Private 
        private float[] _startXPoints;
        private float _startHeight, _canvasWidth;
        [SerializeField] private LevelData _currentLevelData;
        private int _lastHandlerIndex, _currentSpawned;
        private Coroutine _spawnCoroutine;

        #region DATA_STRUCTS

        [Serializable]
        public struct LevelData
        {
            public HandlerData[] levelHandlers;

            public LevelData(int levelSize)
            {
                levelHandlers = new HandlerData[levelSize];
                for (var i = 0; i < levelSize; i++)
                    levelHandlers[i] = new HandlerData(i);
            }

            public void AddObjectData(int handlerIndex, int row_index, int spriteIndex)
            {
                levelHandlers[handlerIndex].AddObjectData(row_index, spriteIndex);
            }

            public bool GetHandler(int handlerIndex, out HandlerData data)
            {
                foreach (var obj in levelHandlers)
                    if (obj.index == handlerIndex)
                    {
                        data = obj;
                        return true;
                    }
                data = default;
                return false;
            }
        }

        [Serializable]
        public struct HandlerData
        {
            public int index;
            public ObjectData[] objectsInRowArray;
            public int count;

            public HandlerData(int index)
            {
                this.index = index;
                this.objectsInRowArray = new ObjectData[3];
                this.count = 0;
            }

            public void AddObjectData(int row_index, int spriteIndex)
            {
                if (!objectsInRowArray[row_index].inited)
                    count++;
                objectsInRowArray[row_index] = new ObjectData(row_index, spriteIndex);
            }
        }

        [Serializable]
        public struct ObjectData
        {
            public int row_index;   //0-1-2 in spawn row
            public int spriteIndex;
            public bool inited;

            public ObjectData(int row_index, int spriteIndex)
            {
                this.row_index = row_index;
                this.spriteIndex = spriteIndex;
                this.inited = true;
            }
        }
        #endregion

        private void Awake()
        {
            Instance = this;
        }

        private IEnumerator Start()
        {
            var canvasRect = ((RectTransform)canvas).rect;
            _canvasWidth = canvasRect.width;
            _startHeight = canvasRect.height + spawnTopHeightOffset; // Screen Height + Offset
            _startXPoints = new float[3];
            for (var i = 0; i < 3; i++)
                _startXPoints[i] = _canvasWidth / 4f * (i + 1);

            GenerateLevelData();

            yield return new WaitForEndOfFrame();
            _lastHandlerIndex = startFrom;
            OnProgressUpdate?.Invoke(_lastHandlerIndex, levelSize);

            _spawnCoroutine = StartCoroutine(TrySpawnNext());
        }

        public void OnDespawn(int handlerIndex)
        {
            if (!_currentLevelData.GetHandler(handlerIndex, out HandlerData data))
            {
                Debug.LogErrorFormat("[GameManager.OnDespawn]: Cannot get data for index `{0}`", handlerIndex);
                return;
            }
            _currentSpawned -= data.count;
            if (_spawnCoroutine == null)
                _spawnCoroutine = StartCoroutine(TrySpawnNext());
            OnProgressUpdate?.Invoke(handlerIndex + 1, levelSize);
        }

        IEnumerator TrySpawnNext()
        {
            while (CanSpawnNext())
            {
                SpawnHandler(_lastHandlerIndex);
                yield return new WaitForSeconds(spawnDelay);
            }
            _spawnCoroutine = null;
        }

        bool CanSpawnNext()
        {
            if (_lastHandlerIndex >= levelSize) return false;
            if (!_currentLevelData.GetHandler(_lastHandlerIndex, out HandlerData data))
            {
                Debug.LogErrorFormat("[GameManager.CanSpawnNext]: Cannot get data for index `{0}`", _lastHandlerIndex);
                return false;
            }
            return _currentSpawned + data.count <= maxTargets;
        }

        void SpawnHandler(int handlerIndex)
        {
            if (!_currentLevelData.GetHandler(handlerIndex, out HandlerData data))
            {
                Debug.LogErrorFormat("[GameManager.SpawnObject]: Cannot get data for index `{0}`", handlerIndex);
                return;
            }

            var handler = Instantiate(objectHandler, canvas)
                .AddComponent<ObjectsHandler>();
            var handlerRect = ((RectTransform)handler.transform);
            handlerRect.sizeDelta = new Vector2(_canvasWidth, 150f);    //MAGIC NUMBER = handler height
            handlerRect.anchoredPosition = new Vector2(_canvasWidth / 2f, _startHeight);
            foreach (var objData in data.objectsInRowArray)
            {
                if (!objData.inited) continue;
                var obj = Instantiate(objectPrefab, handler.transform)
                    .GetComponent<LevelObject>();
                var sprite = spriteList[objData.spriteIndex];
                obj.Init(sprite);
                ((RectTransform)obj.transform).anchoredPosition = new Vector2(_startXPoints[objData.row_index], 0f);
            }
            handler.Init(handlerIndex, handlersSpeed);
            _currentSpawned += data.count;
            _lastHandlerIndex++;
        }


        void GenerateLevelData()
        {
            _currentLevelData = new LevelData(levelSize);

            var indexer = 0;
            while (indexer < levelSize)
            {
                var r = UnityEngine.Random.Range(1, _startXPoints.Length);   //[1, Points Size)

                var points = GetSpawnXIndexes(r);

                foreach (var row_index in points)
                {
                    var sprite_r = UnityEngine.Random.Range(0, spriteList.Count);
                    _currentLevelData.AddObjectData(indexer, row_index, sprite_r);
                }
                indexer++;
            }
        }

        int[] GetSpawnXIndexes(int count)
        {
            if (count <= 0 || count > _startXPoints.Length)
            {
                Debug.LogErrorFormat("[GameManager.Spawn]: Wrong spawn points count `{0}`", count);
                return default;
            }

            var uniqueList = new List<int>();
            for (var i = 0; i < _startXPoints.Length; i++)
                uniqueList.Add(i);

            var array = new int[count];
            var c = 0;
            while (c < count)
            {
                var r = UnityEngine.Random.Range(0, uniqueList.Count);
                array[c] = uniqueList[r];
                uniqueList.RemoveAt(r);
                c++;
            }
            return array;
        }
    }
}

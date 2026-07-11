using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstForm
{
    /// <summary>
    /// 저장과 런타임에서 공통으로 사용하는 아이템 ID와 중첩 수입니다.
    /// </summary>
    [Serializable]
    public class RunItemStackData
    {
        public string itemId = string.Empty;
        public int stackCount;

        public RunItemStackData()
        {
        }

        public RunItemStackData(string id, int count)
        {
            itemId = id ?? string.Empty;
            stackCount = Mathf.Max(0, count);
        }

        public RunItemStackData Clone()
        {
            return new RunItemStackData(itemId, stackCount);
        }
    }

    /// <summary>
    /// 현재 회차에만 유지되는 지속형 전리품과 중첩 수를 관리합니다.
    /// </summary>
    [Serializable]
    public class RunInventoryData
    {
        public List<RunItemStackData> items = new List<RunItemStackData>();

        public int GetStackCount(string itemId)
        {
            RunItemStackData stack = FindStack(itemId);
            return stack != null ? Mathf.Max(0, stack.stackCount) : 0;
        }

        /// <summary>
        /// 최대 중첩 전이라면 한 개를 추가하고 새 중첩 수를 반환합니다.
        /// </summary>
        public bool TryAdd(ItemData item, out int newStackCount)
        {
            newStackCount = 0;
            if (item == null || item.IsImmediate)
            {
                return false;
            }

            EnsureList();
            RunItemStackData stack = FindStack(item.itemId);
            int currentCount = stack != null ? Mathf.Max(0, stack.stackCount) : 0;
            if (currentCount >= item.maxStacks)
            {
                newStackCount = currentCount;
                return false;
            }

            if (stack == null)
            {
                stack = new RunItemStackData(item.itemId, 0);
                items.Add(stack);
            }

            stack.stackCount++;
            newStackCount = stack.stackCount;
            return true;
        }

        /// <summary>
        /// 저장된 중첩을 카탈로그의 최대치 안에서 복원합니다.
        /// </summary>
        public void SetStackFromSave(ItemData item, int stackCount)
        {
            if (item == null || item.IsImmediate || stackCount <= 0)
            {
                return;
            }

            EnsureList();
            int safeCount = Mathf.Clamp(stackCount, 1, item.maxStacks);
            RunItemStackData stack = FindStack(item.itemId);
            if (stack == null)
            {
                items.Add(new RunItemStackData(item.itemId, safeCount));
            }
            else
            {
                stack.stackCount = safeCount;
            }
        }

        public List<RunItemStackData> CloneStacks()
        {
            EnsureList();
            List<RunItemStackData> clones = new List<RunItemStackData>();
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && !string.IsNullOrEmpty(items[i].itemId) && items[i].stackCount > 0)
                {
                    clones.Add(items[i].Clone());
                }
            }

            return clones;
        }

        public void Clear()
        {
            EnsureList();
            items.Clear();
        }

        private RunItemStackData FindStack(string itemId)
        {
            EnsureList();
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].itemId == itemId)
                {
                    return items[i];
                }
            }

            return null;
        }

        private void EnsureList()
        {
            if (items == null)
            {
                items = new List<RunItemStackData>();
            }
        }
    }
}

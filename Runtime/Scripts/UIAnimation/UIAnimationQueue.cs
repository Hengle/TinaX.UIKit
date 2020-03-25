﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UniRx;

namespace TinaX.UIKit.Animation
{
    [AddComponentMenu("TinaX/UIKit/Animation/Queue")]
    public class UIAnimationQueue : UIAnimationBase
    {
        public List<QueueItem> Queues;

        [Serializable]
        public struct QueueItem
        {
            public AniItem[] UI_Anis;
            public float DelayAfter;
        }

        [Serializable]
        public struct AniItem
        {
            public UIAnimationBase UI_Ani;
            public bool ReadyOnQueueStart;
        }

        private int index = 0;
        private bool play_flag = false;

        public override void Play()
        {
            //检查死循环
            if (checkLoopRecursive(this))
                return;

            if (this.Queues == null || this.Queues.Count == 0) 
                return;

            play_flag = true;
            doPlay();


            base.Play();
        }

        public override void Stop()
        {
            if (!play_flag) return;
            play_flag = false;
            if(index < Queues.Count)
            {
                if(Queues[index].UI_Anis != null && Queues[index].UI_Anis.Length > 0)
                {
                    foreach (var item in Queues[index].UI_Anis)
                    {
                        if (item.UI_Ani == null)
                            continue;
                        item.UI_Ani.Stop();
                    }
                }
                
            }
            base.Stop();
        }

        //递归
        private void doPlay()
        {
            if (!play_flag) return;
            if (index >= this.Queues.Count)
                return;


            if (Queues[index].UI_Anis == null || Queues[index].UI_Anis.Length == 0)
                finish();
            else
            {
                int _play_counter = 0;
                int counter = 0;
                void __finish()
                {
                    counter++;
                    Debug.Log("收到finish" + counter);
                    if (counter == _play_counter)
                    {
                        Debug.Log("队列中的当前index都finish了：" + index);
                        foreach (var item in Queues[index].UI_Anis)
                        {
                            if (item.UI_Ani == null) continue;
                            item.UI_Ani.onFinish.RemoveListener(__finish);
                        }
                        this.finish();
                    }
                }
                foreach(var item in Queues[index].UI_Anis)
                {
                    if (item.UI_Ani == null) continue;
                    item.UI_Ani.pingPong = false;
                    item.UI_Ani.onFinish.AddListener(__finish);
                    item.UI_Ani.Play();
                    _play_counter++;
                }
                if (_play_counter == 0)
                    this.finish();
            }


        }

        private void finish()
        {
            if (index >= this.Queues.Count)
            {
                return;
            }
            Debug.Log("finish尝试等待并继续执行下一队列，index:" + index);
            //等待并继续开始
            Observable
                .NextFrame()
                .Delay(TimeSpan.FromSeconds(this.Queues[index].DelayAfter))
                .Subscribe(_ =>
                {
                    Debug.Log("finish的等待结束了, index "+ index);
                    if (!play_flag) return;
                    index++;
                    if(index >= this.Queues.Count)
                    {
                        Debug.Log("到此，队列结束");
                        //队列结束
                        play_flag = false;
                        this.AniFinish();
                        return;
                    }
                    doPlay();
                });
        }

        /// <summary>
        /// 递归 死循环检查
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="calls"></param>
        /// <returns>发现死循环则返回true</returns>
        private bool checkLoopRecursive(UIAnimationQueue queue, List<UIAnimationQueue> calls = null)
        {
            if (calls == null)
                calls = new List<UIAnimationQueue>();
            calls.Add(queue);

            if (queue.Queues == null || queue.Queues.Count == 0) return false;

            foreach(var item in queue.Queues)
            {
                if (item.UI_Anis == null || item.UI_Anis.Length == 0)
                    continue;
                foreach(var item2 in item.UI_Anis)
                {
                    if (item2.UI_Ani == null)
                        continue;
                    if (!(item2.UI_Ani is UIAnimationQueue))
                        continue;
                    var __queue = item2.UI_Ani as UIAnimationQueue;
                    if(calls.Contains(__queue))
                    {
                        //死循环了
                        Debug.LogError($"[TinaX.UIKit.UIAnimationQueue] The queue cannot be played because the current queue or one of the queues contains the current queue, causing an infinite loop.The queue causing the conflict is {__queue.name}", __queue);
                        return true;
                    }
                    //递归检查
                    bool b = checkLoopRecursive(__queue, calls);
                    if (b)
                        return true;
                }
            }

            return false;
        }

    }
}
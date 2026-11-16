using UnityEngine;

namespace ChiecLopCuoi.Gameplay
{
    /// <summary>
    /// Gọi Animator an toàn: chỉ SetTrigger/Play khi Controller thật sự có
    /// parameter/state tương ứng, tránh các lỗi console dạng
    /// "Parameter 'KO' does not exist" hoặc "State could not be found".
    /// </summary>
    public static class AnimatorSafe
    {
        /// <summary>Kiểm tra Animator có parameter (Trigger/Bool/Float/Int) với tên cho trước hay không.</summary>
        public static bool HasParameter(Animator animator, string parameterName)
        {
            if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(parameterName))
            {
                return false;
            }

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == parameterName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Kiểm tra Animator có state với tên cho trước trong layer chỉ định hay không.</summary>
        public static bool HasState(Animator animator, string stateName, int layerIndex = 0)
        {
            if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(stateName))
            {
                return false;
            }

            return animator.HasState(layerIndex, Animator.StringToHash(stateName));
        }

        /// <summary>Chỉ gọi SetTrigger khi parameter tồn tại. Trả về true nếu đã gọi.</summary>
        public static bool TrySetTrigger(Animator animator, string triggerName)
        {
            if (!HasParameter(animator, triggerName))
            {
                return false;
            }

            animator.SetTrigger(triggerName);
            return true;
        }

        /// <summary>Chỉ gọi Play khi state tồn tại. Trả về true nếu đã gọi.</summary>
        public static bool TryPlayState(Animator animator, string stateName, int layerIndex = 0, float normalizedTime = 0f)
        {
            if (!HasState(animator, stateName, layerIndex))
            {
                return false;
            }

            animator.Play(stateName, layerIndex, normalizedTime);
            return true;
        }
    }
}

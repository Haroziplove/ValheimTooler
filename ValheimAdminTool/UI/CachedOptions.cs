using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimAdminTool.UI
{
    // Dropdown labels for IMGUI popups. Rebuilds the array only when the source list changes
    // (or every couple of seconds, to pick up renames), instead of on every IMGUI event.
    public sealed class CachedOptions
    {
        private object m_source;
        private int m_count = -1;
        private float m_builtAt;
        private string[] m_values;

        public string[] Get<T>(ICollection<T> source, Func<T, string> label)
        {
            if (source == null)
            {
                m_source = null;
                m_count = -1;
                m_values = null;
                return null;
            }

            if (m_values == null || !ReferenceEquals(source, m_source) || source.Count != m_count || Time.unscaledTime - m_builtAt > 2f)
            {
                string[] values = new string[source.Count];
                int i = 0;
                foreach (T item in source)
                {
                    values[i++] = label(item);
                }

                m_values = values;
                m_source = source;
                m_count = source.Count;
                m_builtAt = Time.unscaledTime;
            }

            return m_values;
        }
    }
}

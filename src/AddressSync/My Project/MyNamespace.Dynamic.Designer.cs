using System;
using System.ComponentModel;
using System.Diagnostics;

namespace FlsGliderSync.My
{
    internal static partial class MyProject
    {
        internal partial class MyForms
        {
            [EditorBrowsable(EditorBrowsableState.Never)]
            public FrmDBAuswahl m_FrmDBAuswahl;

            public FrmDBAuswahl FrmDBAuswahl
            {
                [DebuggerHidden]
                get
                {
                    m_FrmDBAuswahl = Create__Instance__(m_FrmDBAuswahl);
                    return m_FrmDBAuswahl;
                }

                [DebuggerHidden]
                set
                {
                    if (ReferenceEquals(value, m_FrmDBAuswahl))
                        return;
                    if (value is object)
                        throw new ArgumentException("Property can only be set to Nothing");
                    Dispose__Instance__(ref m_FrmDBAuswahl);
                }
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public FrmFlightImport m_FrmFlightImport;

            public FrmFlightImport FrmFlightImport
            {
                [DebuggerHidden]
                get
                {
                    m_FrmFlightImport = Create__Instance__(m_FrmFlightImport);
                    return m_FrmFlightImport;
                }

                [DebuggerHidden]
                set
                {
                    if (ReferenceEquals(value, m_FrmFlightImport))
                        return;
                    if (value is object)
                        throw new ArgumentException("Property can only be set to Nothing");
                    Dispose__Instance__(ref m_FrmFlightImport);
                }
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public FrmMain m_FrmMain;

            public FrmMain FrmMain
            {
                [DebuggerHidden]
                get
                {
                    m_FrmMain = Create__Instance__(m_FrmMain);
                    return m_FrmMain;
                }

                [DebuggerHidden]
                set
                {
                    if (ReferenceEquals(value, m_FrmMain))
                        return;
                    if (value is object)
                        throw new ArgumentException("Property can only be set to Nothing");
                    Dispose__Instance__(ref m_FrmMain);
                }
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public FrmSettings m_FrmSettings;

            public FrmSettings FrmSettings
            {
                [DebuggerHidden]
                get
                {
                    m_FrmSettings = Create__Instance__(m_FrmSettings);
                    return m_FrmSettings;
                }

                [DebuggerHidden]
                set
                {
                    if (ReferenceEquals(value, m_FrmSettings))
                        return;
                    if (value is object)
                        throw new ArgumentException("Property can only be set to Nothing");
                    Dispose__Instance__(ref m_FrmSettings);
                }
            }
        }
    }
}
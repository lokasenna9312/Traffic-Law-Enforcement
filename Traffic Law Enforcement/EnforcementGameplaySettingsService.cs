namespace Traffic_Law_Enforcement
{
    public static class EnforcementGameplaySettingsService
    {
        private static EnforcementGameplaySettingsState s_Current = EnforcementGameplaySettingsState.CreateCodeDefaults();

        public static EnforcementGameplaySettingsState Current => s_Current;

        public static void Apply(EnforcementGameplaySettingsState state)
        {
            s_Current = state;
        }

        public static void ResetToCodeDefaults()
        {
            s_Current = EnforcementGameplaySettingsState.CreateCodeDefaults();
        }
    }
}

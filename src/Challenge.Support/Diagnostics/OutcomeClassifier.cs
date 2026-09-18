namespace Challenge.Support.Diagnostics;

public static class OutcomeClassifier
{
    public static string Classify(bool failed, string outcome, string phase, bool demo, bool observation, string message)
    {
        if (!failed) return observation ? "Contract observation; product decision pending" : outcome;
        if (message.Contains("ServiceUnavailableException")) return "Environment / external dependency";
        if (phase == "arrange") return "Setup failure; scenario not exercised";
        // A demo tag alone cannot make a missing browser or unrelated failure an intentional success.
        if (demo && message.Contains("EXPECTED DEMO FAILURE:")) return "Intentional diagnostics demonstration";
        if (message.Contains("ContractException")) return "API contract mismatch; triage required";
        if (message.Contains("Timeout", StringComparison.OrdinalIgnoreCase)) return "Timeout; product / test / environment triage required";
        return "Business assertion mismatch; product / test triage required";
    }
}

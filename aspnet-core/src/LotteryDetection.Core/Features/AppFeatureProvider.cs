using Abp.Application.Features;
using Abp.Localization;
using Abp.Runtime.Validation;
using Abp.UI.Inputs;

namespace LotteryDetection.Features;

public class AppFeatureProvider : FeatureProvider
{
    public override void SetFeatures(IFeatureDefinitionContext context)
    {
        context.Create(
            AppFeatures.MaxUserCount,
            "0", //0 = unlimited
            L("MaximumUserCount"),
            L("MaximumUserCount_Description"),
            inputType: new SingleLineStringInputType(new NumericValueValidator(0))
        )[FeatureMetadata.CustomFeatureKey] = new FeatureMetadata
        {
            ValueTextNormalizer = value => value == "0" ? L("Unlimited") : new FixedLocalizableString(value),
            IsVisibleOnPricingTable = true
        };

        #region ######## Example Features - You can delete them #########

        context.Create("TestTenantScopeFeature", "false", L("TestTenantScopeFeature"), scope: FeatureScopes.Tenant);
        context.Create("TestEditionScopeFeature", "false", L("TestEditionScopeFeature"), scope: FeatureScopes.Edition);

        context.Create(
            AppFeatures.TestCheckFeature,
            "false",
            L("TestCheckFeature"),
            inputType: new CheckboxInputType()
        )[FeatureMetadata.CustomFeatureKey] = new FeatureMetadata
        {
            IsVisibleOnPricingTable = true,
            TextHtmlColor = value => value == "true" ? "#5cb85c" : "#d9534f"
        };

        context.Create(
            AppFeatures.TestCheckFeature2,
            "true",
            L("TestCheckFeature2"),
            inputType: new CheckboxInputType()
        )[FeatureMetadata.CustomFeatureKey] = new FeatureMetadata
        {
            IsVisibleOnPricingTable = true,
            TextHtmlColor = value => value == "true" ? "#5cb85c" : "#d9534f"
        };

        #endregion

        var chatFeature = context.Create(
            AppFeatures.ChatFeature,
            "false",
            L("ChatFeature"),
            inputType: new CheckboxInputType()
        );

        chatFeature.CreateChildFeature(
            AppFeatures.TenantToTenantChatFeature,
            "false",
            L("TenantToTenantChatFeature"),
            inputType: new CheckboxInputType()
        );

        chatFeature.CreateChildFeature(
            AppFeatures.TenantToHostChatFeature,
            "false",
            L("TenantToHostChatFeature"),
            inputType: new CheckboxInputType()
        );
    }

    private static ILocalizableString L(string name)
    {
        return new LocalizableString(name, LotteryDetectionConsts.LocalizationSourceName);
    }
}
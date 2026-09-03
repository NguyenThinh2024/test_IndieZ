# Thinh Template SDK

This package contains the reusable SDK and provider-neutral gameplay template
under `Assets/_SDK`.

## Export

In Unity, select `Tools > Thinh Template > Export Package`.
The package is written to `Exports/ThinhTemplate.unitypackage`.

Use `Tools > Thinh Template > Export Full Package` to include gameplay
prefabs, required art/audio, DOTween, Odin, Lean GUI, and UI extensions. The
full package is written to `Exports/ThinhTemplateFull.unitypackage` and
excludes all assets under `Assets/VoodooPackages`.

## Required dependencies

- Unity 6000.0 or newer
- Universal Render Pipeline
- TextMesh Pro and Unity UI
- DOTween
- UniTask
- Odin Inspector
- Newtonsoft Json
- UI Particle (`com.coffee.ui-particle`)
- CandyCoded Haptic Feedback

## Optional integrations

The SDK contains adapters for Firebase, GameAnalytics, AppsFlyer, AppLovin MAX,
Google Mobile Ads, TopOn, and Unity IAP. Install and enable only the providers
used by the target project.

Gameplay code exposes state changes through `GameStateService.OnChangeState`.
Analytics integrations should subscribe to gameplay events from a separate
adapter instead of adding provider-specific calls to gameplay services.

## Analytics

`ThinhAnalytics` automatically reports `GameStarted` and `GameFinished` from
the gameplay template. `AnalyticsController` forwards events to the enabled
Firebase, GameAnalytics, and AppsFlyer integrations. Every gameplay event is
also printed to the Console with the `[Thinh.Analytics]` prefix.

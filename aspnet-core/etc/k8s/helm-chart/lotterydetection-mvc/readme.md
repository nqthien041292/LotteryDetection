Install using;

```bash
helm upgrade --install anz lotterydetection-mvc
```

Uninstall all charts

```bash
helm uninstall anz
```

## Create Images

```bash
docker build -t lotterydetection-mvc -f src\LotteryDetection.Web.Mvc\Dockerfile .
docker build -t lotterydetection-migrator -f src\LotteryDetection.Migrator\Dockerfile .
```

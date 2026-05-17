Install using;

```bash
helm upgrade --install anz lotterydetection-angular
```

Uninstall all charts

```bash
helm uninstall anz
```

## Create Images

### run in the aspnet-core folder
```bash
docker build -t lotterydetection-host -f src\LotteryDetection.Web.Host\Dockerfile .
docker build -t lotterydetection-migrator -f src\LotteryDetection.Migrator\Dockerfile .
```

### run in the angular folder
```bash
docker build -t lotterydetection-angular -f Dockerfile . 
```

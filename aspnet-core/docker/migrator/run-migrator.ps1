docker-compose -f docker-compose.migrator.yml up -d
docker logs -f lotterydetectionmigrator_container
docker container rm lotterydetectionmigrator_container
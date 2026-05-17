docker network rm lotterydetection

docker network create lotterydetection
docker-compose -f docker-compose.infrastructure.yml up -d

docker logs -f mssqlDb_container

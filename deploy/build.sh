cd ../CityGuessBot
docker build . -t guess-that-city

docker stop guess-that-city
docker rm guess-that-city

# Setup APP_1 to run
sudo docker run --restart=always --name guess-that-city -d guess-that-city


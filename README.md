# vulture-pay-core-payments


# Mock Amqp

docker commands: 
```sh
docker build -t leandroalves86/amqp.mock:1.0.0.beta01 -f mocks/Amqp.Mock/Dockerfile .
docker tag leandroalves86/amqp.mock:1.0.0.beta01 leandroalves86/amqp.mock:latest

docker push leandroalves86/amqp.mock:1.0.0.beta01
docker push leandroalves86/amqp.mock:latest
```
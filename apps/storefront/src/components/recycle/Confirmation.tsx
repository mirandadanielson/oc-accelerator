import {
    Button,
    Container,
    Heading,
    Icon,
    Text,
  } from "@chakra-ui/react";
  import { Link } from "react-router-dom";
  import { TbCircleCheck } from "react-icons/tb";
  
  export const Confirmation = (): JSX.Element => {
  
    return (
      <Container centerContent size="xl">
      <Heading size="lg" m={5}><Icon color="green" mr={3} as={TbCircleCheck} />Thank you for your trade-in! Your order helps support our initiatives to reduce our environmental impact by allowing us to refurbish and resell used goods.</Heading>
      <Text>We look forward to seeing you in store to process your trade-in.</Text>
      <Button as={Link} to={'/products'} m={5}>Continue Shopping</Button>
      </Container>
    );
  };
  
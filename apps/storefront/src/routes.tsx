import { RouteObject } from "react-router-dom";
import Layout from "./Layout/Layout";
import Dashboard from './components/Dashboard';
import { OrderSummary } from "./components/cart/OrderSummary";
import { ShoppingCart } from "./components/cart/ShoppingCart";
import CategoryList from "./components/category/CategoryList";
import ProductDetailWrapper from "./components/product/ProductDetailWrapper";
import RecycleDetailWrapper from "./components/recycle/ProductDetailWrapper";
import ProductList from "./components/product/ProductList";
import RecycleList from "./components/recycle/ProductList";
import { Confirmation } from "./components/recycle/Confirmation";

const routes: RouteObject[] = [
  {
    path: "/",
    element: <Layout />,
    children: [
      {
        path: "/",
        element: <Dashboard />,
      },
      {
        path: "/cart",
        element: <ShoppingCart />,
      },
      { path: "/order-summary", element: <OrderSummary /> },
      {
        path: "/products",
        element: <ProductList />,
      },
      {
        path: "/categories/:catalogId",
        element: <CategoryList />,
      },
      {
        path: "/product-list/:catalogId",
        element: <ProductList />,
      },
      {
        path: "/product-list/:catalogId/:categoryId",
        element: <ProductList />,
      },
      {
        path: "/products/:productId",
        element: <ProductDetailWrapper />,
      },
      {
        path: "/trade-in/:catalogId",
        element: <RecycleList />
      },
      {
        path: "/trade-in-product/:productId",
        element: <RecycleDetailWrapper />
      },
      {
        path: "/trade-in-confirmation",
        element: <Confirmation />
      }
    ],
  },
];

export default routes;

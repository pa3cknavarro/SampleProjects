import type { ComponentType } from 'react';
import type { AuthUser, CustomerProfile, Order } from '../../src/api';

declare const CustomerMenu: ComponentType<{
  user: AuthUser | null;
  customer: CustomerProfile | undefined;
  orders: Order[];
  onCustomerUpdate: (profile: CustomerProfile) => void;
  onLogout: () => void;
}>;

export default CustomerMenu;

import AuthLayout from '@/components/AuthLayout';

export default function UserNotRegisteredError() {
  return (
    <AuthLayout
      title="Account not registered"
      subtitle="Your account is not registered for this app. Contact the app owner to request access."
    />
  );
}

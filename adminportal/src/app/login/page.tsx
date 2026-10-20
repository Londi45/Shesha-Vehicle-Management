"use client";

import React from "react";
import Link from "next/link";
import {
  ConfigurableForm,
  PageWithLayout,
} from "@shesha-io/reactjs";
import { ACTIVE_LOGIN } from "@/app-constants/layout";

interface IProps {}

const Login: PageWithLayout<IProps> = () => (
  <>
    <ConfigurableForm
      mode={"edit"}
      formId={ACTIVE_LOGIN}
    />
    <div style={{ textAlign: "center", marginTop: 16 }}>
      Don&apos;t have an account?{" "}
      <Link href="/no-auth/Boxfusion.VehicleBooking/Sign-up">Sign up</Link>
    </div>
  </>
);

export default Login;
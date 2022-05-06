use std::ops::{Add, AddAssign, Mul, MulAssign};

#[derive(Clone, Copy, Debug, Default, PartialEq, PartialOrd)]
pub struct LogSpace(pub f32);

fn log_add_exp(a: f32, b: f32) -> f32 {
    if a == f32::NEG_INFINITY {
        return b;
    } else if b == f32::NEG_INFINITY {
        return a;
    }

    if a > b {
        return a + f32::ln_1p(f32::exp(b - a));
    } else {
        return b + f32::ln_1p(f32::exp(a - b));
    }
}

impl Add for LogSpace {
    type Output = LogSpace;

    fn add(self, rhs: Self) -> Self::Output {
        LogSpace(log_add_exp(self.0, rhs.0))
    }
}

impl Add<f32> for LogSpace {
    type Output = LogSpace;

    fn add(self, rhs: f32) -> Self::Output {
        LogSpace(log_add_exp(self.0, rhs))
    }
}

impl AddAssign for LogSpace {
    fn add_assign(&mut self, rhs: Self) {
        self.0 = log_add_exp(self.0, rhs.0);
    }
}

impl AddAssign<f32> for LogSpace {
    fn add_assign(&mut self, rhs: f32) {
        self.0 = log_add_exp(self.0, rhs);
    }
}

impl Mul for LogSpace {
    type Output = LogSpace;

    fn mul(self, rhs: Self) -> Self::Output {
        LogSpace(self.0 + rhs.0)
    }
}

impl Mul<f32> for LogSpace {
    type Output = LogSpace;

    fn mul(self, rhs: f32) -> Self::Output {
        LogSpace(self.0 + rhs)
    }
}

impl MulAssign for LogSpace {
    fn mul_assign(&mut self, rhs: Self) {
        self.0 += rhs.0;
    }
}

impl MulAssign<f32> for LogSpace {
    fn mul_assign(&mut self, rhs: f32) {
        self.0 += rhs;
    }
}

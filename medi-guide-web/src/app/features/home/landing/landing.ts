import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { RouterLink, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { TranslationService } from '../../../core/services/translation';
import { ThemeService } from '../../../core/services/theme';
import { TestimonialService } from '../../../core/services/testimonial';
import { Testimonial } from '../../../core/models/testimonial.model';
import { AuthService } from '../../../core/services/auth';
import { FeedbackService } from '../../../core/services/feedback.service';

export interface HeroSlide {
  image: string;
  prefix: string;
  highlight: string;
  suffix: string;
  prefixAm: string;
  highlightAm: string;
  suffixAm: string;
  desc: string;
  descAm: string;
}

export interface SpecialtyItem {
  id: string;
  tagline: string;
  taglineAm: string;
  title: string;
  titleAm: string;
  desc: string;
  descAm: string;
  icon: string;
  image: string;
}

export interface TeamMember {
  name: string;
  role: string;
  roleAm: string;
  department: string;
  departmentAm: string;
  image: string;
  bio: string;
  bioAm: string;
  icon: string;
}

export interface ArticleItem {
  title: string;
  titleAm: string;
  date: string;
  dateAm: string;
  category: string;
  categoryAm: string;
  image: string;
  summary: string;
  summaryAm: string;
}

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [
    RouterLink,
    FormsModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatTooltipModule,
    MatSelectModule,
    MatFormFieldModule,
  ],
  templateUrl: './landing.html',
  styleUrl: './landing.scss',
})
export class Landing implements OnInit, OnDestroy {
  readonly i18n = inject(TranslationService);
  readonly themeService = inject(ThemeService);
  private readonly testimonialService = inject(TestimonialService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly feedbackService = inject(FeedbackService);

  isLoggedIn = this.auth.isLoggedIn;
  user = this.auth.currentUser;

  testimonials = signal<Testimonial[]>([]);
  loadingTestimonials = signal(false);

  newsletterEmail = signal('');
  searchQuery = '';

  // Dynamic Background Images & Synchronized Text for the Hero section
  heroSlides: HeroSlide[] = [
    {
      image: '/assets/hero-medicare.jpg',
      prefix: 'THE',
      highlight: 'RIGHT',
      suffix: 'PEDIATRICIAN',
      prefixAm: 'ትክክለኛው',
      highlightAm: 'የህጻናት',
      suffixAm: 'ስፔሻሊስት',
      desc: 'We at Medi-Guide are always fully focused on helping your child and you to overcome any hurdle, diagnostic challenge, or healthcare need.',
      descAm: 'በሜዲ-ጋይድ እርስዎ እና ልጅዎ ማንኛውንም የህክምና ችግር፣ የተወሳሰበ ምርመራ ወይም ፈተና በፍጥነት እንዲያልፉ በትኩረት እንሰራለን።',
    },
    {
      image: 'https://images.unsplash.com/photo-1519494026892-80bbd2d6fd0d?auto=format&fit=crop&w=1920&q=80',
      prefix: 'ADVANCED',
      highlight: 'CARDIAC',
      suffix: '& CLINICAL CARE',
      prefixAm: 'የላቀ',
      highlightAm: 'የልብና ክሊኒካል',
      suffixAm: 'ህክምና እንክብካቤ',
      desc: 'Connect with certified cardiologists for precise diagnostic reviews, preventive care, and proactive heart health management for you and your family.',
      descAm: 'የተረጋገጡ የልብ ስፔሻሊስቶች እና ክሊኒካል አማካሪዎች የኢሲጂ (ECG) ንባብ፣ የመከላከያ እንክብካቤ እና አስተማማኝ ህክምና ያገኛሉ።',
    },
    {
      image: 'https://images.unsplash.com/photo-1576091160399-112ba8d25d1d?auto=format&fit=crop&w=1920&q=80',
      prefix: 'EXPERT',
      highlight: 'SECOND',
      suffix: 'OPINIONS',
      prefixAm: 'የታመነ',
      highlightAm: 'የሁለተኛ',
      suffixAm: 'አስተያየት ድጋፍ',
      desc: 'Upload MRI scans, pathology reports, and lab results for rigorous independent reviews by top-tier specialists before undertaking major surgery.',
      descAm: 'ከፍተኛ ቀዶ ህክምና ከማድረግዎ በፊት የኤምአርአይ (MRI)፣ የላብራቶሪ እና የፓቶሎጂ ውጤቶችዎን በከፍተኛ ስፔሻሊስቶች ያስገምግሙ።',
    },
    {
      image: 'https://images.unsplash.com/photo-1551076805-e1869033e561?auto=format&fit=crop&w=1920&q=80',
      prefix: 'COORDINATED',
      highlight: 'DIASPORA',
      suffix: 'HEALTHCARE',
      prefixAm: 'አስተማማኝ',
      highlightAm: 'የዲያስፖራ',
      suffixAm: 'ቤተሰብ ህክምና',
      desc: 'Bridging distance with compassionate healthcare navigation: oversee, coordinate, and sponsor verified medical care for your family in Ethiopia.',
      descAm: 'በውጭ ሀገር ሆነው በኢትዮጵያ ለሚገኙ ቤተሰቦችዎ ጥራት ያለው የህክምና ክትትል፣ መድኃኒትና ቀጠሮ በቀላሉ ያስተባብሩ።',
    },
  ];

  heroImages: string[] = this.heroSlides.map((s) => s.image);
  currentHeroIndex = signal(0);
  private heroInterval?: any;

  // 3 Specialty Cards from Adapted Design
  specialties: SpecialtyItem[] = [
    {
      id: 'pediatrics',
      tagline: 'Caring for our young generation',
      taglineAm: 'ለልጆችና ቤተሰብ ሁለንተናዊ እንክብካቤ',
      title: 'PEDIATRICIAN',
      titleAm: 'የህጻናት ስፔሻሊስት',
      desc: 'Comprehensive developmental assessments, neonatal care, and specialist pediatric consultations with gentle, family-centered guidance.',
      descAm: 'የህጻናት እድገት ምርመራ፣ የጨቅላ ህጻናት ህክምና እና የልዩ ባለሙያ ምክክር በታማኝ ሀኪሞች።',
      icon: 'child_care',
      image: 'https://images.unsplash.com/photo-1622253692010-333f2da6031d?auto=format&fit=crop&w=600&q=80',
    },
    {
      id: 'cardiology',
      tagline: 'Comprehensive cardiovascular care',
      taglineAm: 'የልብና የደም ዝውውር ጤና ድጋፍ',
      title: 'CARDIOLOGIST',
      titleAm: 'የልብ ስፔሻሊስት',
      desc: 'Advanced ECG reviews, hypertension management, and specialist cardiovascular guidance for preventive and acute cardiac wellness.',
      descAm: 'የልብ ምትና የደም ግፊት ምርመራ፣ የላቀ የህክምና ግምገማ እና የካርዲዮሎጂ ምክር።',
      icon: 'monitor_heart',
      image: 'https://images.unsplash.com/photo-1576091160550-2173dba999ef?auto=format&fit=crop&w=600&q=80',
    },
    {
      id: 'neurology',
      tagline: 'Complex neuro & spine care',
      taglineAm: 'የነርቭና አከርካሪ ህክምና መፍትሄዎች',
      title: 'NEUROLOGIST',
      titleAm: 'የነርቭ ስፔሻሊስት',
      desc: 'Diagnostic MRI evaluations, expert second opinions, and coordinated care pathways for neurological and spine conditions.',
      descAm: 'የኤምአርአይ (MRI) ምስል ትንተና፣ የሁለተኛ አስተያየት ግምገማ እና የነርቭ ህክምና ቅንጅት።',
      icon: 'psychology',
      image: 'https://images.unsplash.com/photo-1584515979956-d9f6e5d09982?auto=format&fit=crop&w=600&q=80',
    },
  ];

  // 4 Featured Doctors from Adapted Team Grid
  team: TeamMember[] = [
    {
      name: 'Dr. Brook Solomon',
      role: 'Senior Consultant Neurologist',
      roleAm: 'ከፍተኛ የነርቭ ስፔሻሊስት',
      department: 'Neurology & Diagnostics',
      departmentAm: 'የነርቭና ምርመራ ዘርፍ',
      image: 'https://images.unsplash.com/photo-1622902046580-2b47f47f5471?auto=format&fit=crop&w=600&q=80',
      bio: '14+ years experience leading complex neurology triage, diagnostic MRI assessments, and patient pathway planning.',
      bioAm: 'ከ14 ዓመታት በላይ በነርቭ ህክምና፣ በኤምአርአይ ግምገማ እና በከፍተኛ የህክምና ቅንጅት ያገለገሉ።',
      icon: 'psychology',
    },
    {
      name: 'Dr. Selamawit Desta',
      role: 'Consultant Cardiologist',
      roleAm: 'የልብና ደም ስሮች ስፔሻሊስት',
      department: 'Cardiovascular Health',
      departmentAm: 'የልብ ህክምና ዘርፍ',
      image: 'https://images.unsplash.com/photo-1594824813589-9a285871fa0c?auto=format&fit=crop&w=600&q=80',
      bio: 'Specialist in echocardiogram review, preventive cardiovascular care, and acute hypertension therapy.',
      bioAm: 'የልብ ጤና ምርመራ፣ የአልትራሳውንድ ግምገማ እና የደም ግፊት መቆጣጠር ስፔሻሊስት።',
      icon: 'monitor_heart',
    },
    {
      name: 'Dr. Yohannes Tadesse',
      role: 'Senior Pediatric Specialist',
      roleAm: 'ከፍተኛ የህጻናት ስፔሻሊስት',
      department: 'Pediatrics & Neonatal',
      departmentAm: 'የህጻናትና ጨቅላ ህክምና',
      image: 'https://images.unsplash.com/photo-1537368910025-700350fe46c7?auto=format&fit=crop&w=600&q=80',
      bio: 'Dedicated to newborn intensive health, pediatric developmental screening, and parent advisory support.',
      bioAm: 'የህጻናት እድገት፣ የጨቅላ ህጻናት አስቸኳይ እንክብካቤ እና የቤተሰብ ጤና ባለሙያ።',
      icon: 'child_care',
    },
    {
      name: 'Dr. Bethlehem Hailu',
      role: 'Internal Medicine Lead',
      roleAm: 'የውስጥ ደዌ ስፔሻሊስት',
      department: 'Clinical Diagnostics Lead',
      departmentAm: 'የውስጥ ደዌና ምርመራ መሪ',
      image: 'https://images.unsplash.com/photo-1559839734-2b71ea197ec2?auto=format&fit=crop&w=600&q=80',
      bio: 'Championing rigorous diagnostic evaluations, second opinion reviews, and personalized treatment roads.',
      bioAm: 'ለተወሳሰቡ የውስጥ ደዌ ህመሞች ገለልተኛ ሁለተኛ አስተያየት እና የህክምና ማስተባበር መሪ።',
      icon: 'medical_services',
    },
  ];

  // 3 Articles from Adapted News Grid
  latestNews: ArticleItem[] = [
    {
      title: 'Top Diagnostic & Hospital Centers in Addis Ababa',
      titleAm: 'በአዲስ አበባ ከፍተኛ የዲያግኖስቲክስ እና የሆስፒታል ማዕከላት',
      date: 'September 2026',
      dateAm: 'መስከረም 2019',
      category: 'Healthcare Navigation',
      categoryAm: 'የጤና መመሪያ',
      image: 'https://images.unsplash.com/photo-1519494026892-80bbd2d6fd0d?auto=format&fit=crop&w=600&q=80',
      summary: 'Navigating verified private and tertiary public healthcare institutions with accredited diagnostic facilities in Ethiopia.',
      summaryAm: 'በአዲስ አበባ የሚገኙ እውቅና ያላቸው የግልና የመንግስት ከፍተኛ ሆስፒታሎችን የመምረጥ መመሪያ።',
    },
    {
      title: 'Are Second Opinions Essential for Complex Diagnosis?',
      titleAm: 'ለተወሳሰበ የህክምና ምርመራ ሁለተኛ አስተያየት ምን ያህል አስፈላጊ ነው?',
      date: 'September 2026',
      dateAm: 'መስከረም 2019',
      category: 'Clinical Advice',
      categoryAm: 'ክሊኒካል ምክር',
      image: 'https://images.unsplash.com/photo-1579684385127-1ef15d508118?auto=format&fit=crop&w=600&q=80',
      summary: 'How independent physician review eliminates diagnostic uncertainty and prevents unnecessary surgical procedures.',
      summaryAm: 'ገለልተኛ የህክምና ግምገማ ያልተረጋገጡ ቀዶ-ህክምናዎችንና የተሳሳቱ ምርመራዎችን እንዴት እንደሚከላከል የሚገልጽ ትንታኔ።',
    },
    {
      title: 'Diaspora Family Health: Managing Care from Abroad',
      titleAm: 'የዲያስፖራው ማህበረሰብ፡ የቤተሰብን ጤና ከውጭ ሀገር ሆኖ ማስተባበር',
      date: 'August 2026',
      dateAm: 'ነሐሴ 2018',
      category: 'Diaspora Coordination',
      categoryAm: 'የዲያስፖራ ቅንጅት',
      image: 'https://images.unsplash.com/photo-1588776814546-1ffcf47267a5?auto=format&fit=crop&w=600&q=80',
      summary: 'Safe, coordinated health management for parents and loved ones in Ethiopia with transparent hospital reporting.',
      summaryAm: 'በውጭ የሚኖሩ ኢትዮጵያውያን በሀገር ውስጥ ለሚገኙ ወላጆቻቸውና ቤተሰቦቻቸው ቀጥተኛ ክትትልና ክፍያ የሚያደርጉበት መንገድ።',
    },
  ];

  scrollToSection(id: string) {
    const el = document.getElementById(id);
    if (el) {
      el.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
  }

  setHeroIndex(idx: number) {
    this.currentHeroIndex.set(idx);
    this.restartHeroSlider();
  }

  ngOnInit() {
    this.loadTestimonials();
    this.startHeroSlider();
  }

  ngOnDestroy() {
    this.stopHeroSlider();
  }

  private startHeroSlider() {
    this.heroInterval = setInterval(() => {
      this.currentHeroIndex.update((prev) => (prev + 1) % this.heroSlides.length);
    }, 5500);
  }

  private restartHeroSlider() {
    this.stopHeroSlider();
    this.startHeroSlider();
  }

  private stopHeroSlider() {
    if (this.heroInterval) {
      clearInterval(this.heroInterval);
      this.heroInterval = undefined;
    }
  }

  loadTestimonials() {
    this.loadingTestimonials.set(true);
    this.testimonialService.getApproved().subscribe({
      next: (list) => {
        if (list && list.length > 0) {
          this.testimonials.set(list);
        } else {
          this.testimonials.set([
            {
              id: '1',
              patientName: 'Abeba T.',
              rating: 5,
              comment: 'Medi-Guide connected my father with a senior neurologist and arranged hospital bed delivery within 24 hours. Invaluable care coordination.',
              createdAt: '2026-08-15',
            },
            {
              id: '2',
              patientName: 'Dawit M.',
              rating: 5,
              comment: 'Finding an authentic FMHACA-licensed traditional healer with verified background gave my family complete peace of mind.',
              createdAt: '2026-08-28',
            },
            {
              id: '3',
              patientName: 'Sara K. (Diaspora - US)',
              rating: 5,
              comment: 'Organizing medical travel and specialized treatment planning from abroad was completely seamless through their messaging portal.',
              createdAt: '2026-09-02',
            },
          ]);
        }
        this.loadingTestimonials.set(false);
      },
      error: () => {
        this.loadingTestimonials.set(false);
      },
    });
  }

  getDashboardLink(): string {
    const roles = this.auth.roles();
    if (roles.includes('Admin')) return '/admin';
    if (roles.includes('Agent')) return '/agent';
    return '/patient';
  }

  onSpecialtySelected(specialtyId: string) {
    if (this.isLoggedIn()) {
      this.router.navigate([this.getDashboardLink()]);
    } else {
      this.router.navigate(['/register']);
    }
  }

  onSubscribeNewsletter() {
    const email = this.newsletterEmail().trim();
    if (!email || !email.includes('@')) {
      this.feedbackService.error(
        this.i18n.isAmharic()
          ? 'እባክዎ ትክክለኛ የኢሜይል አድራሻ ያስገቡ።'
          : 'Please provide a valid email address.',
        this.i18n.isAmharic() ? 'ልክ ያልሆነ ኢሜይል' : 'Invalid Email'
      );
      return;
    }

    this.newsletterEmail.set('');
    this.feedbackService.success(
      this.i18n.isAmharic()
        ? 'ለሜዲ-ጋይድ የጤና መረጃዎች በተሳካ ሁኔታ ተመዝግበዋል። እናመሰግናለን!'
        : 'You have successfully subscribed to Medi-Guide health updates and clinical guides. Thank you!',
      this.i18n.isAmharic() ? 'ምዝገባው ተሳክቷል' : 'Subscribed'
    );
  }

  onSearch() {
    const query = this.searchQuery?.trim();
    if (query) {
      this.scrollToSection('services');
    }
  }
}
